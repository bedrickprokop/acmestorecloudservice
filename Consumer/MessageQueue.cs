using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Consumer
{
    public class MessageQueue
    {
        public Int32 productId { get; set; }
        public Boolean fromNotification { get; set; }
        public String fromView { get; set; }
        public User user { get; set; }
        public String message { get; set; }
    }
}
