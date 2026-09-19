using BMWMS.Repository.Models;
using Microsoft.EntityFrameworkCore;

namespace BMWMS.Business.Common;

public static class OrderWorkflowLock
{
    public static Task AcquireAsync(BmwmsContext context, string kind, long id) =>
        context.Database.ExecuteSqlInterpolatedAsync($@"
DECLARE @result int;
EXEC @result = sys.sp_getapplock @Resource = {"BMWMS:" + kind + ":" + id},
 @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;
IF @result < 0 THROW 51140, N'Phiếu đang được xử lý ở phiên khác. Vui lòng tải lại và thử lại.', 1;");
}
