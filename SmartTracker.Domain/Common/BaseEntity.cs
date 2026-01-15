namespace SmartTracker.Domain.Common
{
    public  class BaseEntity
    {
        public Guid Id { get; protected set; }

        public DateTime CreatedAt { get; protected set; }
        public DateTime UpdatedAt { get; protected set; }

        public bool IsDeleted { get; protected set; }
        public bool IsActive { get; protected set; }
        public bool IsArchived { get; protected set; }
        public bool IsPublished { get; protected set; }

        protected BaseEntity()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            IsActive = true;
        }


        public void MarkUpdated()
        {
            UpdatedAt = DateTime.UtcNow;
        }

        public void SoftDelete()
        {
            IsDeleted = true;
            IsActive = false;
            MarkUpdated();
        }

        public void Restore()
        {
            IsDeleted = false;
            IsActive = true;
            MarkUpdated();
        }

        public void Archive()
        {
            IsArchived = true;
            IsActive = false;
            MarkUpdated();
        }

        public void Activate()
        {
            if (IsDeleted || IsArchived)
                throw new InvalidOperationException("Cannot activate deleted or archived entity.");

            IsActive = true;
            MarkUpdated();
        }

        public void Deactivate()
        {
            IsActive = false;
            MarkUpdated();
        }

        public void Publish()
        {
            if (IsDeleted || IsArchived)
                throw new InvalidOperationException("Cannot publish deleted or archived entity.");

            IsPublished = true;
            MarkUpdated();
        }

        public void UnPublish()
        {
            IsPublished = false;
            MarkUpdated();
        }
    }
}
