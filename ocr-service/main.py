import asyncio
import base64
import json
import uuid
from io import BytesIO

import aio_pika
import pytesseract
from PIL import Image

# Path to Tesseract executable (update if installed elsewhere)
pytesseract.pytesseract.tesseract_cmd = r"C:\Program Files\Tesseract-OCR\tesseract.exe"

RABBITMQ_URL = "amqp://guest:guest@localhost/"
QUEUE_NAME = "ocr-requested"  # must match the queue MassTransit publishes to


async def process_message(message: aio_pika.IncomingMessage):
    async with message.process():
        try:
            body = json.loads(message.body.decode())

            # MassTransit wraps the payload in a "message" key
            payload = body.get("message", body)
            image_base64 = payload.get("imageBase64", "")
            client_id = payload.get("clientId", "")
            response_address = body.get("responseAddress", "")
            request_id = body.get("requestId", str(uuid.uuid4()))

            print(f"[OCR] Processing image for client: {client_id}")

            # Decode base64 image and run OCR
            image_bytes = base64.b64decode(image_base64)
            image = Image.open(BytesIO(image_bytes))
            extracted_text = pytesseract.image_to_string(image)

            print(f"[OCR] Extracted text: {extracted_text[:100]}...")

            # Build MassTransit-compatible response envelope
            response = {
                "messageId": str(uuid.uuid4()),
                "requestId": request_id,
                "messageType": ["urn:message:DealTrack.Application.Contracts:OcrCompleted"],
                "message": {
                    "extractedText": extracted_text.strip(),
                    "clientId": client_id,
                    "success": True,
                    "errorMessage": None
                }
            }

            # Extract reply queue from responseAddress
            # e.g. rabbitmq://localhost/QUEUE_NAME
            reply_queue = response_address.split("/")[-1] if response_address else None

            if reply_queue:
                channel = await message.channel.connection.channel()
                await channel.default_exchange.publish(
                    aio_pika.Message(
                        body=json.dumps(response).encode(),
                        content_type="application/json",
                        correlation_id=request_id
                    ),
                    routing_key=reply_queue
                )
                print(f"[OCR] Reply sent to: {reply_queue}")
            else:
                print("[OCR] No responseAddress found, skipping reply.")

        except Exception as ex:
            print(f"[OCR] Error: {ex}")

            # Send failure response if possible
            try:
                body = json.loads(message.body.decode())
                response_address = body.get("responseAddress", "")
                request_id = body.get("requestId", "")
                client_id = body.get("message", {}).get("clientId", "")
                reply_queue = response_address.split("/")[-1] if response_address else None

                if reply_queue:
                    error_response = {
                        "messageId": str(uuid.uuid4()),
                        "requestId": request_id,
                        "messageType": ["urn:message:DealTrack.Application.Contracts:OcrCompleted"],
                        "message": {
                            "extractedText": "",
                            "clientId": client_id,
                            "success": False,
                            "errorMessage": str(ex)
                        }
                    }
                    channel = await message.channel.connection.channel()
                    await channel.default_exchange.publish(
                        aio_pika.Message(
                            body=json.dumps(error_response).encode(),
                            content_type="application/json"
                        ),
                        routing_key=reply_queue
                    )
            except Exception:
                pass


async def main():
    print("[OCR Service] Connecting to RabbitMQ...")
    connection = await aio_pika.connect_robust(RABBITMQ_URL)

    async with connection:
        channel = await connection.channel()
        await channel.set_qos(prefetch_count=1)

        queue = await channel.declare_queue(QUEUE_NAME, durable=True)
        print(f"[OCR Service] Listening on queue: {QUEUE_NAME}")

        await queue.consume(process_message)
        await asyncio.Future()  # Run forever


if __name__ == "__main__":
    asyncio.run(main())
