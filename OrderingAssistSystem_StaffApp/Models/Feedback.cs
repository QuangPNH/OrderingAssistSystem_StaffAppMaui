using System;
using System.Collections.Generic;

namespace OrderingAssistSystem_StaffApp.Models
{
    public partial class Feedback
    {
        public int Feedbackid { get; set; }
        public string? Content { get; set; }
        public int? MemberId { get; set; }

        public virtual Member? Member { get; set; }
    }
}
