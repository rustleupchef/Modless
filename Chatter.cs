namespace Modless;

public class Chatter
{
    public int messages { get; set; }
    public int time { get; set; }

    // higher level invokation
    public Chatter(int messages, string time)
    {
        this.messages = messages;
        string[] units = time.Split(':');
        this.time = int.Parse(units[0]) * 3600 + // hours
                    int.Parse(units[1]) * 60 + // minutes
                    int.Parse(units[2]); // seconds
    }

    // manually entering time
    public Chatter(int messages, int time)
    {
        this.messages = messages;
        this.time = time;
    }

    // defaults
    public Chatter()
    {
        messages = 0;
        time = 0;
    }

    public int difference(string time)
    {
        string[] units = time.Split(':');
        return difference(int.Parse(units[0]) * 3600 + // hours
                          int.Parse(units[1]) * 60 + // minutes
                          int.Parse(units[2])); // seconds
    }

    public int difference(int time)
    {
        return Math.Abs(time - this.time);
    }
}