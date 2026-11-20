
namespace OrderManagement.Application.Features.Orders.GetMyOrders;

public sealed record GetMyOrdersRequest(
    int PageNumber = 1,
    int PageSize = 10);
