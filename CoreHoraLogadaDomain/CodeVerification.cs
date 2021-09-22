using CoreHoraLogadaInfra.Configurations;
using CoreHoraLogadaInfra.Models;
using System;
using System.Threading.Tasks;
using System.Timers;

namespace CoreHoraLogadaDomain
{
    public class CodeVerification : IDisposable
    {
        private bool disposed;
        private int elapsedSeconds = 0;
        public RoleAnswerControl roleControl = new RoleAnswerControl();        

        public delegate Task AddHour(RoleAnswerControl roleControl);
        public delegate Task FailNotification(RoleAnswerControl roleControl);

        private readonly Definitions _definitions;

        public CodeVerification(AddHour AddHour, FailNotification FailNotification, Role role, string code, Definitions definitions)
        {
            this._definitions = definitions;

            roleControl.Role = role;
            roleControl.Code = code;
            roleControl.RoleTimer = new System.Timers.Timer(1000);
            roleControl.RoleTimer.Elapsed += (sender, e) => AnswerWatch(sender, e, AddHour, FailNotification);
            roleControl.RoleTimer.Start();            
        }        

        public async Task RoleAnswerTrigger(string roleAnswer)
        {
            if (roleAnswer != null)
                this.roleControl.LastAnswer = roleAnswer;
        }

        private async void AnswerWatch(object sender, ElapsedEventArgs e, AddHour AddHour, FailNotification FailNotification)
        {
            if (++elapsedSeconds > _definitions.TimeToAnswer)
            {
                await FailNotification(this.roleControl);
                this.Dispose(true);
            }                

            if (roleControl != null && roleControl.LastAnswer != null && roleControl.LastAnswer.Equals(roleControl.Code))
            {
                await AddHour(this.roleControl);
                this.roleControl.RoleTimer.Stop();
                this.Dispose(true);
            }
        }
        ~CodeVerification()
        {
            this.Dispose(false);
        }
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    roleControl.RoleTimer.Stop();
                    roleControl.RoleTimer = null;
                    roleControl = null;
                }
            }

            disposed = true;
        }
    }
}
