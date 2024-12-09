namespace WerewolfParty_Server.Extensions;

public static class ListExtension
{
    private static readonly Random rng = new Random();  

    public static List<T> Shuffle<T>(this IList<T> list)  
    {  
        var newList = new List<T>(list);
        int n = list.Count;  
        while (n > 1) {  
            n--;  
            int k = rng.Next(n + 1);  
            (newList[k], newList[n]) = (newList[n], newList[k]);
        }

        return newList;
    }
}