using POS_system_cs.Application.Navigation;
using POS_system_cs.Application.Services;
using POS_system_cs.Configuration;
using POS_system_cs.Infrastructure.Persistence;
using POS_system_cs.Infrastructure.Services;
using POS_system_cs.UI.Forms;

namespace POS_system_cs.Infrastructure;

public static class AppCompositionRoot
{
    private static readonly AppSettings Settings = new();
    private static readonly SqliteConnectionFactory ConnectionFactory = new(Settings);
    private static readonly DatabaseInitializer DatabaseInitializer = new(ConnectionFactory);

    private static readonly ICategoryService CategoryService = new CategoryService(ConnectionFactory);
    private static readonly IProductService ProductService = new ProductService(ConnectionFactory);
    private static readonly IInventoryService InventoryService = new InventoryService(ConnectionFactory);
    private static readonly ICashierService CashierService = new CashierService(ConnectionFactory);
    private static readonly IOrderService OrderService = new OrderService(ConnectionFactory);
    private static readonly IReportService ReportService = new ReportService(ConnectionFactory);

    public static async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await DatabaseInitializer.InitializeAsync(cancellationToken);
    }

    public static MainForm CreateMainForm()
    {
        return new MainForm(CreateModules(), CategoryService, ProductService, InventoryService, CashierService, OrderService, ReportService);
    }

    private static IReadOnlyList<ModuleDefinition> CreateModules()
    {
        return
        [
            new("cashier", "前台收银", "商品录入、购物车、结算、订单保存与库存扣减。", ["录入商品", "结算", "挂单/清空"]),
            new("products", "商品管理", "维护商品基础资料、条码、售价、上下架状态。", ["新增商品", "编辑商品", "搜索商品"]),
            new("categories", "分类管理", "维护商品分类，并校验分类和商品的关联。", ["新增分类", "编辑分类"]),
            new("inventory", "库存管理", "查看库存、手动调整、入库出库和低库存预警。", ["库存调整", "入库登记", "低库存"]),
            new("orders", "销售记录", "查询销售单、查看明细、按日期或商品筛选。", ["订单查询", "查看明细"]),
            new("reports", "统计报表", "统计销售额、订单数、热销商品和库存概况。", ["今日统计", "月报", "导出"]),
            new("settings", "系统设置", "维护店铺信息、数据库、小票打印和系统参数。", ["店铺信息", "打印设置"])
        ];
    }
}
