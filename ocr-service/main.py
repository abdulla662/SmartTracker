import asyncio
import json
import uuid
import os
import base64
import sys
import io
import re
from dotenv import load_dotenv

load_dotenv()

sys.stdout.reconfigure(encoding='utf-8', errors='replace')
sys.stderr.reconfigure(encoding='utf-8', errors='replace')

import aio_pika
from mistralai.client import Mistral
from PIL import Image, ImageEnhance, ImageFilter

# ─── Config ───────────────────────────────────────────────────────────────────
RABBITMQ_URL  = "amqp://guest:guest@localhost/"
QUEUE_NAME    = "ocr-requested"
EXCHANGE_NAME = "DealTrack.Application.Contracts:OcrRequested"

MISTRAL_API_KEY = os.environ.get("MISTRAL_API_KEY", "")
client = Mistral(api_key=MISTRAL_API_KEY)

print("[OCR] Mistral OCR client ready.", flush=True)

ARABIC_RE  = re.compile(r'[؀-ۿݐ-ݿࢠ-ࣿﭐ-﷿ﹰ-﻿]')
PHONE_RE   = re.compile(r'^(\+?20|0)(1[0-9]{9}|[0-9]{6,14})$')
NAME_CLEAN = re.compile(r'[^A-Za-z\s\-\.\']')   # only letters, spaces, hyphen, dot, apostrophe


# ─── Image preprocessing ──────────────────────────────────────────────────────
def preprocess(image_bytes: bytes) -> bytes:
    img = Image.open(io.BytesIO(image_bytes)).convert('RGB')

    # Upscale small images — OCR accuracy improves significantly on larger images
    w, h = img.size
    if max(w, h) < 2500:
        scale = 2500 / max(w, h)
        img = img.resize((int(w * scale), int(h * scale)), Image.LANCZOS)

    # Boost contrast so ink pops against paper background
    img = ImageEnhance.Contrast(img).enhance(1.8)
    # Increase sharpness to make character edges crisp
    img = ImageEnhance.Sharpness(img).enhance(2.0)
    # Two sharpen passes for handwritten text
    img = img.filter(ImageFilter.SHARPEN)
    img = img.filter(ImageFilter.SHARPEN)

    buf = io.BytesIO()
    img.save(buf, format='JPEG', quality=97)
    return buf.getvalue()


# ─── Step 1: OCR — extract raw text with mistral-ocr-latest ──────────────────
def run_ocr(image_bytes: bytes) -> str:
    b64 = base64.b64encode(image_bytes).decode('utf-8')
    response = client.ocr.process(
        model="mistral-ocr-latest",
        document={"type": "image_url", "image_url": f"data:image/jpeg;base64,{b64}"},
    )
    pages_text = "\n".join(p.markdown for p in response.pages if p.markdown)
    print(f"[OCR] Raw OCR text:\n{pages_text}", flush=True)
    return pages_text


# ─── Step 2: Parse — extract structured clients with mistral-small ────────────
def parse_clients(ocr_text: str) -> list:
    if not ocr_text.strip():
        return []

    # Fast Arabic check before paying for LLM call
    arabic_chars = len(ARABIC_RE.findall(ocr_text))
    total_chars  = len([c for c in ocr_text if c.isalpha()])
    if total_chars > 0 and arabic_chars / total_chars > 0.3:
        raise ValueError("Arabic text is not supported. Please write client data in English.")

    prompt = f"""Extract all client entries from the following OCR text.
Each client has a Name and optionally a Phone number.
The text may use labels like "Name:", "Phone:", "Tel:", or just list them line by line.

Return ONLY a JSON array, no markdown fences, no explanation:
[{{"name": "Full Name", "phone": "number or empty string"}}, ...]

If no clients found, return [].

OCR TEXT:
{ocr_text}"""

    response = client.chat.complete(
        model="mistral-small-latest",
        messages=[{"role": "user", "content": prompt}],
        temperature=0,
        max_tokens=800,
    )

    raw = response.choices[0].message.content.strip()
    raw = re.sub(r'^```[a-z]*\n?', '', raw)
    raw = re.sub(r'\n?```$', '', raw)
    raw = raw.strip()

    print(f"[OCR] Parse response: {raw}", flush=True)

    try:
        parsed = json.loads(raw)
    except json.JSONDecodeError as e:
        print(f"[OCR] JSON parse error: {e}", flush=True)
        return []

    if not isinstance(parsed, list):
        return []

    seen_phones = set()
    result = []
    for c in parsed:
        name  = str(c.get("name",  "")).strip()
        phone = str(c.get("phone", "")).strip()

        # Clean name: remove digits and special chars that shouldn't appear
        name = NAME_CLEAN.sub('', name).strip()
        # Normalise phone: strip spaces/dashes
        phone = re.sub(r'[\s\-\(\)]', '', phone)

        # Must have a name of at least 2 real chars
        if len(name) < 2:
            continue

        # Validate phone format if provided
        if phone and not PHONE_RE.match(phone):
            phone = ''   # keep the client but drop the bad phone

        # Deduplicate phones within this batch
        if phone:
            if phone in seen_phones:
                phone = ''   # keep client, just clear duplicate phone
            else:
                seen_phones.add(phone)

        result.append({"name": name, "phone": phone})

    return result


# ─── Main extract function ─────────────────────────────────────────────────────
def extract_clients(image_bytes: bytes) -> list:
    enhanced = preprocess(image_bytes)
    ocr_text = run_ocr(enhanced)
    clients  = parse_clients(ocr_text)
    print(f"[OCR] Final clients: {clients}", flush=True)
    return clients


def build_extracted_text(clients: list) -> str:
    return "\n".join(
        f"{c.get('name','').strip()} {c.get('phone','').strip()}"
        for c in clients
    )


# ─── Reply helper ──────────────────────────────────────────────────────────────
async def send_reply(channel: aio_pika.Channel, response_address: str, request_id: str, msg: dict):
    if not response_address:
        print("[OCR] No responseAddress — cannot reply", flush=True)
        return

    reply_queue = response_address.rstrip("/").split("/")[-1].split("?")[0]
    print(f"[OCR] Replying to queue: {reply_queue}", flush=True)

    envelope = {
        "messageId":   str(uuid.uuid4()),
        "requestId":   request_id,
        "messageType": ["urn:message:DealTrack.Application.Contracts:OcrCompleted"],
        "message":     msg,
    }

    await channel.default_exchange.publish(
        aio_pika.Message(
            body=json.dumps(envelope, ensure_ascii=False).encode('utf-8'),
            content_type="application/json",
            correlation_id=request_id,
        ),
        routing_key=reply_queue,
    )
    print(f"[OCR] Reply sent to {reply_queue}", flush=True)


# ─── Message handler ───────────────────────────────────────────────────────────
def make_handler(channel: aio_pika.Channel):
    async def handle(message: aio_pika.IncomingMessage):
        async with message.process():
            body_raw = message.body.decode()
            print(f"[OCR] Message received ({len(body_raw)} bytes)", flush=True)

            response_address = ""
            request_id       = str(uuid.uuid4())
            client_id        = ""

            try:
                body             = json.loads(body_raw)
                payload          = body.get("message", body)
                image_base64     = payload.get("imageBase64", "")
                client_id        = payload.get("clientId", "")
                file_name        = payload.get("fileName", "image.jpg").lower()
                response_address = body.get("responseAddress", "")
                request_id       = body.get("requestId", request_id)

                print(f"[OCR] clientId={client_id} file={file_name}", flush=True)

                if not image_base64:
                    raise ValueError("imageBase64 is empty")

                image_bytes = base64.b64decode(image_base64)

                loop    = asyncio.get_event_loop()
                clients = await loop.run_in_executor(None, extract_clients, image_bytes)
                print(f"[OCR] Extracted {len(clients)} client(s)", flush=True)

                await send_reply(channel, response_address, request_id, {
                    "extractedText": build_extracted_text(clients),
                    "clientId":      client_id,
                    "success":       True,
                    "errorMessage":  None,
                })

            except Exception as ex:
                import traceback; traceback.print_exc()
                await send_reply(channel, response_address, request_id, {
                    "extractedText": "",
                    "clientId":      client_id,
                    "success":       False,
                    "errorMessage":  str(ex),
                })

    return handle


# ─── Entry point ───────────────────────────────────────────────────────────────
async def main():
    print("[OCR] Connecting to RabbitMQ...", flush=True)

    connection = await aio_pika.connect_robust(RABBITMQ_URL)

    async with connection:
        channel = await connection.channel()
        await channel.set_qos(prefetch_count=1)

        exchange = await channel.declare_exchange(
            EXCHANGE_NAME,
            aio_pika.ExchangeType.FANOUT,
            durable=True,
        )
        queue = await channel.declare_queue(QUEUE_NAME, durable=True)
        await queue.bind(exchange)

        print(f"[OCR] Queue '{QUEUE_NAME}' bound to exchange '{EXCHANGE_NAME}'", flush=True)
        print("[OCR] Listening...", flush=True)

        await queue.consume(make_handler(channel))
        await asyncio.Future()


if __name__ == "__main__":
    asyncio.run(main())
