using System;
using System.Text;

[Serializable]
public class BunkerDailySummaryData
{
    public int dayNumber;
    public int[] collectedAmounts;

    public BunkerDailySummaryData()
    {
        int resourceCount = Enum.GetValues(typeof(BunkerResourceType)).Length;
        collectedAmounts = new int[resourceCount];
    }

    public BunkerDailySummaryData(int dayNumber, int[] collectedAmounts)
    {
        this.dayNumber = dayNumber;
        this.collectedAmounts = collectedAmounts != null ? (int[])collectedAmounts.Clone() : new int[Enum.GetValues(typeof(BunkerResourceType)).Length];
    }

    public int GetCollected(BunkerResourceType resourceType)
    {
        int index = (int)resourceType;
        if (collectedAmounts == null || index < 0 || index >= collectedAmounts.Length)
            return 0;

        return collectedAmounts[index];
    }

    public string BuildTitle()
    {
        return $"Se ha sobrevivido al día {dayNumber:00}";
    }

    public string BuildBody()
    {
        StringBuilder builder = new StringBuilder();
        Array resourceValues = Enum.GetValues(typeof(BunkerResourceType));

        for (int i = 0; i < resourceValues.Length; i++)
        {
            BunkerResourceType resourceType = (BunkerResourceType)resourceValues.GetValue(i);
            int amount = GetCollected(resourceType);

            if (amount > 0)
                builder.AppendLine($"Has recolectado {amount} de {BunkerResourceTypeText.GetSummaryName(resourceType)}.");
            else
                builder.AppendLine($"No has recolectado nada de {BunkerResourceTypeText.GetSummaryName(resourceType)}.");
        }

        return builder.ToString().TrimEnd();
    }
}
