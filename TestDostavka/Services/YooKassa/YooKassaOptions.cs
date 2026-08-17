public sealed class YooKassaOptions
{
    public const string SectionName = "YooKassa";

    public string ShopId { get; set; } = null!;

    public string SecretKey { get; set; } = null!;

    public string ApiUrl { get; set; } =
        "https://api.yookassa.ru/v3/";
}