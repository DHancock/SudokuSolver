using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SudokuSolver;

internal class SudokuTraceListener : TraceListener
{
    const int cMaxStoreLength = 1024 * 10;

    private readonly object lockObject = new object();
    private readonly StringBuilder store = new StringBuilder(cMaxStoreLength);
    private readonly Mutex writeMutex;
    private readonly bool autoFlush = true;
    private string Path { get; }

    public SudokuTraceListener(string path) : base(nameof(SudokuTraceListener))
    {
        Path = path;
        writeMutex = new Mutex(initiallyOwned: false, "C604E038-148B-4E44-9C8F-5CCA9B9EFA8F");
    }


    public override bool IsThreadSafe { get; } = true;

    private void WriteInternal(string message)
    {
        try
        {
            if ((store.Length + message.Length) > cMaxStoreLength)
                FlushInternal();

            store.Append(message);
        }
        catch
        {
        }
    }


    private void FlushInternal()
    {
        writeMutex.WaitOne();

        try
        {
            File.AppendAllText(Path, store.ToString());
            store.Clear();
        }
        catch
        {
        }
        finally
        {
            writeMutex.ReleaseMutex();
        }
    }


    public override void Write(string? message)
    {
        if (message is not null)
        {
            message = $"{DateTime.Now:HH\\:mm\\:ss} - {Environment.ProcessId:X8}: {message}";

            lock (lockObject)
            {
                WriteInternal(message);

                if (autoFlush)
                    FlushInternal();
            }
        }
    }

    public override void WriteLine(string? message) => Write(message + Environment.NewLine);

    public override void Flush()
    {
        lock (lockObject)
        {
            if (store.Length > 0)
                FlushInternal();
        }      
    }
}
