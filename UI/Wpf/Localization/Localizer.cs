using System.Globalization;

namespace POS_system_cs.UI.Wpf.Localization;

public enum AppLanguage
{
    English,
    Chinese
}

public static class Localizer
{
    private static readonly Dictionary<string, (string En, string Zh)> Texts = new()
    {
        ["App.Title"] = ("Small Retail POS System", "小型商超 POS 系统"),
        ["App.NavTitle"] = ("POS System", "POS 系统"),
        ["App.Subtitle"] = ("Small retail checkout desk", "小型商超收银台"),
        ["Language"] = ("Language", "语言"),
        ["English"] = ("English", "English"),
        ["Chinese"] = ("简体中文", "简体中文"),
        ["Confirm"] = ("Confirm", "确认"),
        ["Info"] = ("Info", "提示"),
        ["OperationFailed"] = ("Operation failed", "操作失败"),

        ["Module.cashier"] = ("Cashier", "前台收银"),
        ["Module.products"] = ("Products", "商品管理"),
        ["Module.categories"] = ("Categories", "分类管理"),
        ["Module.inventory"] = ("Inventory", "库存管理"),
        ["Module.orders"] = ("Sales Records", "销售记录"),
        ["Module.reports"] = ("Reports", "统计报表"),
        ["Module.settings"] = ("Settings", "系统设置"),
        ["ModuleDesc.settings"] = ("Store information, receipt printer settings, and system options.", "维护店铺信息、数据库、小票打印和系统参数。"),

        ["Category.Title"] = ("Category Management", "分类管理"),
        ["Category.Desc"] = ("Create, edit, and delete product categories.", "新增、编辑和删除商品分类。"),
        ["Category.Form"] = ("Category", "分类信息"),
        ["Category.DeleteConfirm"] = ("Delete selected category?", "确认删除当前分类？"),
        ["Category.NameRequired"] = ("Category name is required.", "分类名称不能为空。"),
        ["Category.DeleteBlocked"] = ("This category is linked to products and cannot be deleted.", "该分类已关联商品，不能删除。"),

        ["Product.Title"] = ("Product Management", "商品管理"),
        ["Product.Desc"] = ("Maintain product data, barcode, prices, category, and active status.", "维护商品资料、条码、价格、分类和启用状态。"),
        ["Product.Form"] = ("Product", "商品信息"),
        ["Product.DeleteConfirm"] = ("Delete or disable selected product?", "确认删除或停用当前商品？"),
        ["Product.CodeRequired"] = ("Product code is required.", "商品编码不能为空。"),
        ["Product.NameRequired"] = ("Product name is required.", "商品名称不能为空。"),
        ["Product.BarcodeRequired"] = ("Product barcode is required.", "商品条码不能为空。"),
        ["Product.SelectCategory"] = ("Select a product category.", "请选择商品分类。"),
        ["Product.NonNegativeValues"] = ("Prices and low-stock threshold cannot be negative.", "价格和库存预警值不能为负数。"),

        ["Inventory.Title"] = ("Inventory Management", "库存管理"),
        ["Inventory.Desc"] = ("View, adjust, and set stock quantities.", "查看库存、按数量调整，或直接设置商品库存。"),
        ["Inventory.Form"] = ("Stock", "库存调整"),
        ["Inventory.SelectProduct"] = ("Select a product.", "请选择商品。"),
        ["Inventory.StockNonNegative"] = ("Stock cannot be negative.", "库存不能为负数。"),
        ["Inventory.AdjustNonZero"] = ("Adjustment quantity cannot be 0.", "调整数量不能为 0。"),
        ["Inventory.ResultNonNegative"] = ("Stock cannot be negative after adjustment.", "库存调整后不能为负数。"),

        ["Orders.Title"] = ("Sales Records", "销售记录"),
        ["Orders.Desc"] = ("Search orders and inspect line items.", "查询销售单并查看订单明细。"),
        ["Orders.FromDateError"] = ("From date cannot be later than to date.", "开始日期不能晚于结束日期。"),
        ["Orders.Summary"] = ("Orders: {0}    Total: {1:N2}    Discount: {2:N2}    Payable: {3:N2}    Received: {4:N2}", "订单数：{0}    商品合计：{1:N2}    优惠：{2:N2}    应收：{3:N2}    实收：{4:N2}"),

        ["Reports.Title"] = ("Reports", "统计报表"),
        ["Reports.Desc"] = ("Review sales, stock, and product rankings.", "查看销售、库存和商品排行。"),

        ["Settings.Title"] = ("Settings", "系统设置"),
        ["Settings.Desc"] = ("Review current store, database, language, and log settings.", "查看当前店铺、数据库、语言和日志设置。"),
        ["Settings.StoreName"] = ("Store name", "店铺名称"),
        ["Settings.DatabasePath"] = ("Database path", "数据库路径"),
        ["Settings.ReceiptPrinter"] = ("Receipt printer", "小票打印机"),
        ["Settings.CurrentLanguage"] = ("Current language", "当前语言"),
        ["Settings.SettingsFile"] = ("Settings file", "设置文件"),
        ["Settings.LanguageSettingsPath"] = ("Language settings file", "语言设置文件"),
        ["Settings.LogDirectory"] = ("Log directory", "日志目录"),
        ["Settings.RuntimeDirectory"] = ("Runtime directory", "运行目录"),
        ["Settings.EditNotice"] = ("Settings on this page can be edited and saved immediately. If the database path changes, the target database will be initialized automatically.", "此页面的设置可以直接编辑并保存。若数据库路径发生变化，目标数据库会自动初始化。"),
        ["Settings.Saved"] = ("Settings saved.", "设置已保存。"),
        ["Settings.StoreNameRequired"] = ("Store name is required.", "店铺名称不能为空。"),
        ["Settings.DatabasePathRequired"] = ("Database path is required.", "数据库路径不能为空。"),
        ["Settings.DatabasePathInvalid"] = ("Database path is invalid.", "数据库路径无效。"),
        ["Settings.DatabasePathFileRequired"] = ("Database path must include a database file name.", "数据库路径必须包含数据库文件名。"),

        ["Field.Name"] = ("Name", "名称"),
        ["Field.Description"] = ("Description", "说明"),
        ["Field.Active"] = ("Active", "启用"),
        ["Field.Code"] = ("Code", "编码"),
        ["Field.Barcode"] = ("Barcode", "条码"),
        ["Field.Category"] = ("Category", "分类"),
        ["Field.Cost"] = ("Cost", "进价"),
        ["Field.Price"] = ("Price", "售价"),
        ["Field.LowStock"] = ("Low stock", "库存预警"),
        ["Field.Product"] = ("Product", "商品"),
        ["Field.Quantity"] = ("Quantity", "数量"),
        ["Field.Reason"] = ("Reason", "原因"),
        ["Field.From"] = ("From", "开始"),
        ["Field.To"] = ("To", "结束"),
        ["Field.Time"] = ("Time", "时间"),
        ["Field.Total"] = ("Total", "合计"),
        ["Field.Payable"] = ("Payable", "应收"),
        ["Field.Discount"] = ("Discount", "优惠"),
        ["Field.Received"] = ("Received", "实收"),
        ["Field.Change"] = ("Change", "找零"),
        ["Field.Payment"] = ("Payment", "支付方式"),
        ["Field.OrderNo"] = ("Order No", "订单号"),
        ["Field.Amount"] = ("Amount", "金额"),
        ["Field.Period"] = ("Period", "周期"),
        ["Field.Sales"] = ("Sales", "销售额"),
        ["Field.Orders"] = ("Orders", "订单数"),
        ["Field.Stock"] = ("Stock", "库存"),
        ["Field.Low"] = ("Low", "预警"),
        ["Field.Qty"] = ("Qty", "数量"),

        ["Action.Save"] = ("Save", "保存"),
        ["Action.New"] = ("New", "新增"),
        ["Action.Edit"] = ("Edit", "编辑"),
        ["Action.Delete"] = ("Delete", "删除"),
        ["Action.Refresh"] = ("Refresh", "刷新"),
        ["Action.Cancel"] = ("Cancel", "取消"),
        ["Action.Apply"] = ("Apply Enter", "应用 Enter"),
        ["Action.Search"] = ("Search", "搜索"),
        ["Action.DeleteDisable"] = ("Delete / Disable", "删除/停用"),
        ["Action.AdjustDelta"] = ("Adjust by delta", "按增减调整"),
        ["Action.SetStock"] = ("Set stock", "直接设置库存"),
        ["Action.Today"] = ("Today", "今天"),
        ["Action.All"] = ("All", "全部"),
        ["Action.Last30"] = ("Last 30 days", "近 30 天"),
        ["Action.ThisMonth"] = ("This month", "本月"),

        ["Section.Orders"] = ("Orders", "销售单"),
        ["Section.Items"] = ("Items", "订单明细"),
        ["Tab.Daily"] = ("Daily", "按日销售"),
        ["Tab.Weekly"] = ("Weekly", "按周销售"),
        ["Tab.Monthly"] = ("Monthly", "按月销售"),
        ["Tab.TopSelling"] = ("Top selling", "热销商品"),
        ["Tab.SlowSelling"] = ("Slow selling", "滞销商品"),
        ["Metric.TodaySales"] = ("Today sales", "今日销售额"),
        ["Metric.TodayOrders"] = ("Today orders", "今日订单数"),
        ["Metric.TodayQuantity"] = ("Today quantity", "今日销售数量"),
        ["Metric.StockQuantity"] = ("Stock quantity", "库存总量"),
        ["Metric.LowStock"] = ("Low stock", "低库存商品"),

        ["Cashier.Title"] = ("Cashier", "前台收银"),
        ["Cashier.Desc"] = ("Enter code, barcode, or a unique product name, then press Enter.", "输入编码、条码或唯一商品名后按 Enter"),
        ["Cashier.Shortcuts"] = ("Shortcuts: F2 focus input / Enter add product / F6 edit quantity / Delete remove selected item / F4 clear cart / F9 checkout / Esc clear input", "快捷键：F2 聚焦输入 / Enter 加入商品 / F6 修改数量 / Delete 移除选中商品 / F4 清空购物车 / F9 收款 / Esc 清空输入"),
        ["Cashier.Add"] = ("Add Enter", "加入 Enter"),
        ["Cashier.Clear"] = ("Clear F4", "清空 F4"),
        ["Cashier.Checkout"] = ("Checkout F9", "收款 F9"),
        ["Cashier.ProductInput"] = ("Product code / barcode / name", "商品编码 / 条码 / 名称"),
        ["Cashier.MultipleProducts"] = ("Multiple products matched. Enter a more accurate code or barcode.", "找到多个匹配商品，请输入更准确的编码或条码。"),
        ["Cashier.ProductNotUnique"] = ("Product not unique", "商品不唯一"),
        ["Cashier.ProductNotFound"] = ("Product not found.", "未找到商品。"),
        ["Cashier.SelectCartItem"] = ("Select an item in the cart first.", "请先选择购物车中的商品。"),
        ["Cashier.EmptyCart"] = ("Cart is empty and cannot be checked out.", "购物车为空，不能结算。"),
        ["Cashier.Success"] = ("Checkout completed. Order no: {0}", "结算完成。订单号：{0}"),
        ["Cashier.SuccessTitle"] = ("Checkout completed", "结算成功"),
        ["Cashier.QuantityTitle"] = ("Edit Quantity", "修改数量"),
        ["Cashier.ProductPrefix"] = ("Product: {0}", "商品：{0}"),
        ["Cashier.QuantityPositive"] = ("Quantity must be greater than 0.", "数量必须大于 0。"),
        ["Cashier.StockInsufficient"] = ("Insufficient stock for {0}. Current: {1:N2}, required: {2:N2}.", "商品 {0} 库存不足。当前库存：{1:N2}，需要：{2:N2}。"),
        ["Cashier.DiscountNonNegative"] = ("Discount amount cannot be negative.", "优惠金额不能为负数。"),
        ["Cashier.DiscountTooLarge"] = ("Discount amount cannot exceed the total item amount.", "优惠金额不能大于商品总额。"),
        ["Cashier.ReceivedTooSmall"] = ("Received amount cannot be less than payable amount.", "实收金额不能小于应收金额。"),
        ["Cashier.PaymentTitle"] = ("Payment", "收款"),
        ["Cashier.PaymentHint"] = ("Shortcuts: Enter complete / F7 cash / F8 online / Esc cancel. Empty amount defaults to remaining due.", "快捷键：Enter 完成 / F7 现金 / F8 线上 / Esc 取消。未输入金额时默认收完剩余应收。"),
        ["Cashier.Complete"] = ("Complete Enter", "完成 Enter"),
        ["Cashier.Online"] = ("Online F8", "线上 F8"),
        ["Cashier.Cash"] = ("Cash F7", "现金 F7"),
        ["Cashier.Cancel"] = ("Cancel Esc", "取消 Esc"),
        ["Cashier.RemainingDue"] = ("There is still an unpaid amount. Continue payment.", "还有未收金额，请继续收款。"),
        ["Cashier.PaymentStatus"] = ("Received: {0:N2}    Cash: {1:N2}    Online: {2:N2}", "已收：{0:N2}    现金：{1:N2}    线上：{2:N2}"),
        ["Payment.Cash"] = ("Cash", "现金"),
        ["Payment.Online"] = ("Online", "线上支付"),
        ["Payment.Mixed"] = ("Mixed", "混合支付"),
        ["Payment.WeChat"] = ("WeChat", "微信"),
        ["Payment.Alipay"] = ("Alipay", "支付宝"),
        ["Payment.BankCard"] = ("Bank card", "银行卡"),

        ["Database.TestCategoryNotFound"] = ("Test category was not found: {0}", "未找到测试分类：{0}"),
        ["Database.SchemaNotFound"] = ("Database schema script Data/schema.sql was not found.", "未找到数据库建表脚本 Data/schema.sql。"),
    };

    public static AppLanguage Current { get; private set; } = AppLanguage.Chinese;

    static Localizer()
    {
        SetLanguage(Current);
    }

    public static void SetLanguage(AppLanguage language)
    {
        Current = language;
        var cultureName = language == AppLanguage.Chinese ? "zh-CN" : "en-US";
        var culture = CultureInfo.GetCultureInfo(cultureName);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    public static string LanguageName(AppLanguage language)
    {
        return language == AppLanguage.Chinese ? T("Chinese") : T("English");
    }

    public static string T(string key)
    {
        return Texts.TryGetValue(key, out var text)
            ? Current == AppLanguage.Chinese ? text.Zh : text.En
            : key;
    }

    public static string Format(string key, params object[] args)
    {
        return string.Format(CultureInfo.CurrentCulture, T(key), args);
    }

    public static string ModuleTitle(string key) => T($"Module.{key}");

    public static string ModuleDescription(string key, string fallback)
    {
        var textKey = $"ModuleDesc.{key}";
        return Texts.ContainsKey(textKey) ? T(textKey) : fallback;
    }
}
