using CoreHoraLogadaInfra.Configurations;
using CoreHoraLogadaInfra.Models;
using System;
using System.Threading.Tasks;

namespace CoreHoraLogadaDomain;

public class CodeVerification : IDisposable
{
    public readonly RoleAnswerControl roleControl = new RoleAnswerControl();
    public delegate Task AddHour(RoleAnswerControl roleControl);
    public delegate Task FailNotification(RoleAnswerControl roleControl);
    private readonly Definitions _definitions;
    private bool disposed;
    private int elapsedSeconds = 0;

    public CodeVerification(AddHour AddHour, FailNotification FailNotification, Role role, string code, Definitions definitions)
    {
        this._definitions = definitions;

        roleControl.Role = role;
        roleControl.Code = code;
        roleControl.RoleTimer = new System.Threading.Timer(_ =>
        {
            AnswerWatch(AddHour, FailNotification);
        }, null, 0, 500);
    }

    public void RoleAnswerTrigger(string roleAnswer)
    {
        if (!string.IsNullOrEmpty(roleAnswer))
            this.roleControl.LastAnswer = roleAnswer;
    }

    private async void AnswerWatch(AddHour AddHour, FailNotification FailNotification)
    {
        if (++elapsedSeconds > _definitions.TimeToAnswer)
        {
            await FailNotification(this.roleControl);
            this.Dispose(true);
        }

        if ((bool)roleControl?.LastAnswer?.Equals(roleControl.Code))
        {
            await AddHour(this.roleControl);
            this.roleControl.RoleTimer.Dispose();
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
                roleControl.RoleTimer.Dispose();
            }
        }

        disposed = true;
    }
}
