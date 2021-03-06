﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Norma.Delta.Models
{
    public class Slot : IEquatable<Slot>
    {
        public string SlotId { get; set; }

        public string Title { get; set; }

        public DateTime StartAt { get; set; }

        public DateTime EndAt { get; set; }

        public string Highlight { get; set; }

        public string HighlightDetail { get; set; }

        public string Description { get; set; }

        public bool IsFirst { get; set; }

        public bool IsLast { get; set; }

        public virtual Channel Channel { get; set; }

        public virtual ICollection<Episode> Episodes { get; set; }

        [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
        public Slot()
        {
            Episodes = new List<Episode>();
        }

        #region Implementation of IEquatable<Slot>

        public bool Equals(Slot other) => SlotId == other.SlotId;

        #endregion
    }
}