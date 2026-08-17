public class ManagerRequestsPageModel
{
    public ManagerRequestsFilterModel Filter { get; set; } = new();

    public IReadOnlyList<ManagerRequestsListModel> Requests { get; set; } = [];
}