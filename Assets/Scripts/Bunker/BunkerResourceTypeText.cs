public static class BunkerResourceTypeText
{
    public static string GetSummaryName(BunkerResourceType resourceType)
    {
        switch (resourceType)
        {
            case BunkerResourceType.Scrap:
                return "chatarra";
            case BunkerResourceType.Electricity:
                return "electricidad";
            case BunkerResourceType.Water:
                return "agua";
            case BunkerResourceType.Food:
                return "comida";
            default:
                return resourceType.ToString();
        }
    }
}
