namespace ea_Tracker.Services
{
    public abstract class Investigator
    {
        public abstract void Start();
        public abstract void Stop();

        protected void Log(string message)
        {
            Console.WriteLine($"[{DateTime.Now}] {message}");
        }
    }
}

