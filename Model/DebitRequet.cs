using System;
using System.Collections.Generic;
using System.Text;

namespace IncendiaGroupTask.Model
{
    public class DebitRequet
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string Direction { get; set; }
        public int Account { get; set; }

    }
}
