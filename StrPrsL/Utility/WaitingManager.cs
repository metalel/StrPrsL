using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StrPrsL.Utility
{
    public class WaitingManager
    {
        private long waitStartTicks;
        public bool WaitInitialized
        {
            get;
            private set;
        } = false;

        public void StartWait()
        {
            WaitInitialized = true;
            waitStartTicks = DateTime.Now.Ticks;
        }

        public bool IsWaitComplete(long length)
        {
            return DateTime.Now.Ticks >= waitStartTicks + length;
        }

        public void StopWait()
        {
            WaitInitialized = false;
        }
    }
}
