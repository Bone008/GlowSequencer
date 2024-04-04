using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlowSequencer.Util;

/// <summary>Helper class to wrap a callback into the IDisposable interface.</summary>
public class ActionDisposable : IDisposable
{
    private readonly Action _disposeAction;

    public ActionDisposable(Action disposeAction)
    {
        _disposeAction = disposeAction;
    }

    public void Dispose()
    {
        _disposeAction();
    }
}
