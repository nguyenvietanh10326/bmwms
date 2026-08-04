-- ==============================================================================
-- BMWMS - CLEANUP & STANDARDIZED SEED SCRIPT (Chuẩn hóa 15 Nhóm VLXD & Thuộc tính EAV)
-- Sử dụng: Mở trong SQL Server Management Studio (SSMS) và nhấn F5 (Execute)
-- Lưu ý: Hãy đảm bảo đang kết nối đúng Database BMWMS (ở Server tương ứng với appsettings.json)
-- ==============================================================================

USE [BMWMS];
GO

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

PRINT N'======================================================================';
PRINT N'>>> BƯỚC 1: DỌN DẸP & ĐỒNG BỘ DỮ LIỆU CŨ SANG CHUẨN 15 NHÓM VLXD';
PRINT N'======================================================================';

-- 1.1. Chèn / Cập nhật 15 nhóm ngành hàng chuẩn (PG01 -> PG15)
MERGE dbo.ProductGroups AS target
USING (VALUES
    ('PG01', N'Xi măng', N'Xi măng PCB30, PCB40, xi măng trắng các loại (Khu A – Kho khô, pallet; FIFO)'),
    ('PG02', N'Thép xây dựng', N'Thép cuộn, thép thanh D10, D16, D20 (Khu B – Giá thép dài; FIFO)'),
    ('PG03', N'Tôn và vật liệu lợp', N'Tôn lạnh, tôn màu, tấm lợp các loại (Khu C – Giá tôn; FIFO)'),
    ('PG04', N'Gạch xây và gạch ốp lát', N'Gạch đỏ, gạch block, gạch ceramic, gạch granite (Khu D – Pallet gạch; FIFO)'),
    ('PG05', N'Cát, đá và vật liệu rời', N'Cát vàng, cát đen, đá 1x2, sỏi xây dựng (Khu E – Bãi vật liệu rời; FIFO)'),
    ('PG06', N'Sơn và chất phủ', N'Sơn nội thất, sơn ngoại thất, sơn lót, sơn chống thấm (Khu F – Kho hóa chất; FEFO, Bắt buộc Lô & HSD)'),
    ('PG07', N'Keo và vật liệu chống thấm', N'Keo dán gạch, silicone, màng chống thấm (Khu G – Kho khô/hóa chất; FEFO, Bắt buộc Lô & HSD)'),
    ('PG08', N'Phụ gia bê tông và hóa chất', N'Phụ gia đông kết nhanh, chống thấm, bảo dưỡng (Khu H – Kho hóa chất; FEFO, Bắt buộc Lô & HSD)'),
    ('PG09', N'Ống và phụ kiện cấp thoát nước', N'Ống PVC, PPR, HDPE, cút, tê, van khóa (Khu I – Giá ống & phụ kiện; FIFO)'),
    ('PG10', N'Thiết bị điện', N'Dây điện, cáp điện, aptomat, ổ cắm, công tắc (Khu J – Kệ thiết bị điện; FIFO)'),
    ('PG11', N'Thiết bị vệ sinh', N'Bồn cầu, lavabo, vòi nước, sen tắm (Khu K – Kệ hàng dễ vỡ; FIFO)'),
    ('PG12', N'Gỗ, ván và vật liệu nội thất', N'Ván MDF, ván ép, gỗ thanh, ván dăm (Khu L – Kho khô; FIFO)'),
    ('PG13', N'Kính và cửa', N'Kính cường lực, cửa nhôm kính, cửa nhựa (Khu M – Giá dựng đứng; FIFO)'),
    ('PG14', N'Ngũ kim và phụ kiện xây dựng', N'Đinh, vít, bu lông, bản lề, khóa (Khu N – Kệ linh kiện nhỏ; FIFO)'),
    ('PG15', N'Dụng cụ và thiết bị thi công', N'Máy khoan, máy cắt, bay, xẻng, thước đo (Khu O – Kệ dụng cụ; FIFO)')
) AS source (GroupCode, GroupName, Description)
ON target.GroupCode = source.GroupCode
WHEN MATCHED THEN
    UPDATE SET 
        target.GroupName = source.GroupName,
        target.Description = source.Description,
        target.Status = 'ACTIVE',
        target.UpdatedAt = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (GroupCode, GroupName, Description, Status, CreatedAt)
    VALUES (source.GroupCode, source.GroupName, source.Description, 'ACTIVE', SYSUTCDATETIME());

-- 1.2. Chuyển liên kết các Product đang trỏ vào nhóm cũ (PG-01, PG-02...) sang nhóm mới chuẩn (PG01, PG02...)
DECLARE @Pg01Id BIGINT = (SELECT TOP 1 ProductGroupID FROM dbo.ProductGroups WHERE GroupCode = 'PG01');
DECLARE @Pg02Id BIGINT = (SELECT TOP 1 ProductGroupID FROM dbo.ProductGroups WHERE GroupCode = 'PG02');
DECLARE @Pg04Id BIGINT = (SELECT TOP 1 ProductGroupID FROM dbo.ProductGroups WHERE GroupCode = 'PG04');
DECLARE @Pg05Id BIGINT = (SELECT TOP 1 ProductGroupID FROM dbo.ProductGroups WHERE GroupCode = 'PG05');
DECLARE @Pg10Id BIGINT = (SELECT TOP 1 ProductGroupID FROM dbo.ProductGroups WHERE GroupCode = 'PG10');

-- Cập nhật lại các sản phẩm mẫu có sẵn trong database để chuẩn tiếng Việt và đúng ProductGroupID
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Products')
BEGIN
    -- Chuyển sản phẩm thuộc PG-01 cũ sang PG01 mới
    UPDATE p
    SET p.ProductGroupID = @Pg01Id
    FROM dbo.Products p
    JOIN dbo.ProductGroups g ON p.ProductGroupID = g.ProductGroupID
    WHERE g.GroupCode IN ('PG-01', 'PG_01', 'CAT-01');

    -- Chuyển sản phẩm thuộc PG-02 cũ sang PG10 (Thiết bị điện) mới
    UPDATE p
    SET p.ProductGroupID = @Pg10Id
    FROM dbo.Products p
    JOIN dbo.ProductGroups g ON p.ProductGroupID = g.ProductGroupID
    WHERE g.GroupCode IN ('PG-02', 'PG_02', 'CAT-02');

    -- Sửa font chữ và gán đúng nhóm cho các mã sản phẩm mẫu
    UPDATE dbo.Products SET ProductName = N'Xi măng Vicem Hà Tiên PCB40', ProductGroupID = @Pg01Id WHERE ProductCode = 'XM001';
    UPDATE dbo.Products SET ProductName = N'Thép cây Hòa Phát D16 CB300V', ProductGroupID = @Pg02Id WHERE ProductCode = 'TH001';
    UPDATE dbo.Products SET ProductName = N'Đá xây dựng 1x2 nghiền', ProductGroupID = @Pg05Id WHERE ProductCode = 'CD002';
    UPDATE dbo.Products SET ProductName = N'Gạch ốp lát Ceramic Viglacera 600x600', ProductGroupID = @Pg04Id WHERE ProductCode = 'GV001';
    UPDATE dbo.Products SET ProductName = N'Dây cáp điện Cadivi 2.5mm²', ProductGroupID = @Pg10Id WHERE ProductCode = 'DX001';
END;

-- 1.3. Xóa các nhóm cũ không dùng đến (nhóm không còn sản phẩm nào trỏ tới)
DELETE FROM dbo.ProductGroups 
WHERE GroupCode IN ('PG-01', 'PG-02', 'PG_01', 'PG_02', 'CAT-01', 'CAT-02', 'CAT-03')
  AND NOT EXISTS (SELECT 1 FROM dbo.Products WHERE ProductGroupID = dbo.ProductGroups.ProductGroupID);

PRINT N'>>> Đã dọn dẹp các nhóm cũ và liên kết sản phẩm sang chuẩn PG01-PG15 thành công!';
GO

PRINT N'======================================================================';
PRINT N'>>> BƯỚC 2: CẬP NHẬT DANH MỤC THUỘC TÍNH KỸ THUẬT (EAV)';
PRINT N'======================================================================';

MERGE dbo.ProductAttributes AS target
USING (VALUES
    ('BRAND', N'Thương hiệu / Nhà sản xuất', 'TEXT', NULL, N'Tên thương hiệu hoặc nhà sản xuất'),
    ('CEMENT_TYPE', N'Loại xi măng', 'OPTION', NULL, N'Chủng loại xi măng'),
    ('BAG_WEIGHT', N'Khối lượng bao', 'NUMBER', N'kg', N'Khối lượng tịnh của bao'),
    ('CEMENT_GRADE', N'Mác xi măng', 'OPTION', NULL, N'Cường độ nén của xi măng'),
    ('DIAMETER', N'Đường kính', 'NUMBER', N'mm', N'Đường kính tiết diện'),
    ('LENGTH', N'Chiều dài', 'NUMBER', N'm', N'Chiều dài tiêu chuẩn'),
    ('WIDTH', N'Chiều rộng', 'NUMBER', N'm', N'Chiều rộng khổ tấm/cuộn'),
    ('THICKNESS', N'Độ dày', 'NUMBER', N'mm', N'Độ dày của vật tư'),
    ('STEEL_STANDARD', N'Tiêu chuẩn thép', 'OPTION', NULL, N'Tiêu chuẩn kỹ thuật thép'),
    ('STEEL_GRADE', N'Mác thép', 'OPTION', NULL, N'Mác thép cường độ'),
    ('COLOR', N'Màu sắc', 'TEXT', NULL, N'Màu sắc hoàn thiện'),
    ('COATING', N'Lớp mạ / Lớp phủ', 'TEXT', NULL, N'Quy cách lớp mạ kẽm, mạ màu'),
    ('DIMENSION', N'Kích thước quy cách', 'TEXT', N'mm', N'Kích thước quy chuẩn (DxRxC)'),
    ('SURFACE_TYPE', N'Loại bề mặt', 'OPTION', NULL, N'Đặc tính bề mặt'),
    ('QUALITY_GRADE', N'Cấp chất lượng', 'OPTION', NULL, N'Phân loại chất lượng sản phẩm'),
    ('MATERIAL_TYPE', N'Loại vật liệu', 'TEXT', NULL, N'Phân loại vật liệu rời'),
    ('GRAIN_SIZE', N'Kích thước hạt', 'TEXT', N'mm', N'Cỡ hạt sàng'),
    ('SOURCE_LOCATION', N'Nguồn khai thác / Xuất xứ', 'TEXT', NULL, N'Mỏ khai thác hoặc nguồn gốc'),
    ('VOLUME', N'Dung tích', 'NUMBER', N'L', N'Thể tích đóng gói'),
    ('PAINT_TYPE', N'Loại sơn', 'OPTION', NULL, N'Phân loại sơn'),
    ('APPLICATION_SURFACE', N'Bề mặt áp dụng', 'TEXT', NULL, N'Loại bề mặt tương thích'),
    ('GLUE_TYPE', N'Loại keo / Màng', 'OPTION', NULL, N'Chủng loại keo dán, màng'),
    ('WEIGHT_NET', N'Khối lượng tịnh', 'NUMBER', N'kg', N'Khối lượng đóng gói'),
    ('USAGE_SCOPE', N'Phạm vi sử dụng', 'TEXT', NULL, N'Khu vực trong nhà/ngoài trời/chống thấm'),
    ('ADDITIVE_TYPE', N'Loại phụ gia', 'OPTION', NULL, N'Chủng loại phụ gia'),
    ('PHYSICAL_FORM', N'Dạng vật chất', 'OPTION', NULL, N'Dạng lỏng, bột, nhũ tương'),
    ('USAGE_RATIO', N'Tỷ lệ sử dụng', 'TEXT', NULL, N'Tỷ lệ trộn khuyến nghị'),
    ('PRESSURE_RATING', N'Áp suất định mức', 'OPTION', NULL, N'Cấp áp lực làm việc'),
    ('MATERIAL', N'Chất liệu', 'TEXT', NULL, N'Chất liệu cấu thành'),
    ('POWER', N'Công suất', 'NUMBER', N'W', N'Công suất định mức'),
    ('VOLTAGE', N'Điện áp định mức', 'OPTION', NULL, N'Điện áp làm việc'),
    ('CURRENT_RATING', N'Dòng điện định mức', 'NUMBER', N'A', N'Cường độ dòng điện'),
    ('CORE_COUNT', N'Số lõi', 'NUMBER', N'Lõi', N'Số ruột dẫn bên trong'),
    ('CROSS_SECTION', N'Tiết diện', 'NUMBER', N'mm²', N'Tiết diện ruột dẫn'),
    ('WOOD_TYPE', N'Loại gỗ / ván', 'OPTION', NULL, N'Chủng loại gỗ công nghiệp/tự nhiên'),
    ('MOISTURE_RESISTANCE', N'Cấp chống ẩm', 'OPTION', NULL, N'Khả năng kháng ẩm'),
    ('GLASS_TYPE', N'Loại kính / cửa', 'OPTION', NULL, N'Quy cách kính hoặc hệ cửa'),
    ('THREAD_TYPE', N'Kiểu ren', 'TEXT', NULL, N'Quy cách ren (ren mịn, thô, tự khoan)'),
    ('PLATING', N'Lớp mạ bề mặt', 'OPTION', NULL, N'Xử lý bề mặt chống rỉ'),
    ('MODEL', N'Model / Mã dòng máy', 'TEXT', NULL, N'Ký hiệu mã máy của nhà SX'),
    ('WARRANTY_MONTHS', N'Thời hạn bảo hành', 'NUMBER', N'Tháng', N'Số tháng bảo hành chính hãng')
) AS source (AttributeCode, AttributeName, DataType, UnitLabel, Description)
ON target.AttributeCode = source.AttributeCode
WHEN MATCHED THEN
    UPDATE SET 
        target.AttributeName = source.AttributeName,
        target.DataType = source.DataType,
        target.UnitLabel = source.UnitLabel,
        target.Description = source.Description,
        target.Status = 'ACTIVE'
WHEN NOT MATCHED THEN
    INSERT (AttributeCode, AttributeName, DataType, UnitLabel, Description, Status)
    VALUES (source.AttributeCode, source.AttributeName, source.DataType, source.UnitLabel, source.Description, 'ACTIVE');

PRINT N'>>> Đã cập nhật xong 41 thuộc tính kỹ thuật.';
GO

-- Nạp Options
DECLARE @AttrId BIGINT;

-- CEMENT_TYPE
SELECT @AttrId = ProductAttributeID FROM dbo.ProductAttributes WHERE AttributeCode = 'CEMENT_TYPE';
IF @AttrId IS NOT NULL
BEGIN
    MERGE dbo.ProductAttributeOptions AS target
    USING (VALUES
        (@AttrId, 'PCB30', N'Xi măng Pooc lăng hỗn hợp PCB30', 1),
        (@AttrId, 'PCB40', N'Xi măng Pooc lăng hỗn hợp PCB40', 2),
        (@AttrId, 'WHITE', N'Xi măng trắng', 3),
        (@AttrId, 'SLAG', N'Xi măng xỉ lò cao / chịu mặn', 4)
    ) AS src (ProductAttributeID, OptionCode, OptionValue, DisplayOrder)
    ON target.ProductAttributeID = src.ProductAttributeID AND target.OptionCode = src.OptionCode
    WHEN MATCHED THEN UPDATE SET target.OptionValue = src.OptionValue, target.DisplayOrder = src.DisplayOrder, target.IsActive = 1
    WHEN NOT MATCHED THEN INSERT (ProductAttributeID, OptionCode, OptionValue, DisplayOrder, IsActive) VALUES (src.ProductAttributeID, src.OptionCode, src.OptionValue, src.DisplayOrder, 1);
END;

-- CEMENT_GRADE
SELECT @AttrId = ProductAttributeID FROM dbo.ProductAttributes WHERE AttributeCode = 'CEMENT_GRADE';
IF @AttrId IS NOT NULL
BEGIN
    MERGE dbo.ProductAttributeOptions AS target
    USING (VALUES
        (@AttrId, '30MPA', N'Mác 30 (>= 30 MPa)', 1),
        (@AttrId, '40MPA', N'Mác 40 (>= 40 MPa)', 2),
        (@AttrId, '50MPA', N'Mác 50 (>= 50 MPa)', 3)
    ) AS src (ProductAttributeID, OptionCode, OptionValue, DisplayOrder)
    ON target.ProductAttributeID = src.ProductAttributeID AND target.OptionCode = src.OptionCode
    WHEN MATCHED THEN UPDATE SET target.OptionValue = src.OptionValue, target.DisplayOrder = src.DisplayOrder, target.IsActive = 1
    WHEN NOT MATCHED THEN INSERT (ProductAttributeID, OptionCode, OptionValue, DisplayOrder, IsActive) VALUES (src.ProductAttributeID, src.OptionCode, src.OptionValue, src.DisplayOrder, 1);
END;

-- STEEL_GRADE
SELECT @AttrId = ProductAttributeID FROM dbo.ProductAttributes WHERE AttributeCode = 'STEEL_GRADE';
IF @AttrId IS NOT NULL
BEGIN
    MERGE dbo.ProductAttributeOptions AS target
    USING (VALUES
        (@AttrId, 'CB240T', N'CB240-T (Thép cuộn trơn)', 1),
        (@AttrId, 'CB300V', N'CB300-V (Thép thanh vằn)', 2),
        (@AttrId, 'CB400V', N'CB400-V (Thép thanh vằn)', 3),
        (@AttrId, 'CB500V', N'CB500-V (Thép cường độ cao)', 4),
        (@AttrId, 'SD295', N'SD295 (Chuẩn JIS)', 5),
        (@AttrId, 'SD390', N'SD390 (Chuẩn JIS)', 6)
    ) AS src (ProductAttributeID, OptionCode, OptionValue, DisplayOrder)
    ON target.ProductAttributeID = src.ProductAttributeID AND target.OptionCode = src.OptionCode
    WHEN MATCHED THEN UPDATE SET target.OptionValue = src.OptionValue, target.DisplayOrder = src.DisplayOrder, target.IsActive = 1
    WHEN NOT MATCHED THEN INSERT (ProductAttributeID, OptionCode, OptionValue, DisplayOrder, IsActive) VALUES (src.ProductAttributeID, src.OptionCode, src.OptionValue, src.DisplayOrder, 1);
END;

-- STEEL_STANDARD
SELECT @AttrId = ProductAttributeID FROM dbo.ProductAttributes WHERE AttributeCode = 'STEEL_STANDARD';
IF @AttrId IS NOT NULL
BEGIN
    MERGE dbo.ProductAttributeOptions AS target
    USING (VALUES
        (@AttrId, 'TCVN1651', N'TCVN 1651-2:2018', 1),
        (@AttrId, 'JIS_G3112', N'JIS G3112 (Nhật Bản)', 2),
        (@AttrId, 'ASTM_A615', N'ASTM A615 (Hoa Kỳ)', 3)
    ) AS src (ProductAttributeID, OptionCode, OptionValue, DisplayOrder)
    ON target.ProductAttributeID = src.ProductAttributeID AND target.OptionCode = src.OptionCode
    WHEN MATCHED THEN UPDATE SET target.OptionValue = src.OptionValue, target.DisplayOrder = src.DisplayOrder, target.IsActive = 1
    WHEN NOT MATCHED THEN INSERT (ProductAttributeID, OptionCode, OptionValue, DisplayOrder, IsActive) VALUES (src.ProductAttributeID, src.OptionCode, src.OptionValue, src.DisplayOrder, 1);
END;

-- SURFACE_TYPE
SELECT @AttrId = ProductAttributeID FROM dbo.ProductAttributes WHERE AttributeCode = 'SURFACE_TYPE';
IF @AttrId IS NOT NULL
BEGIN
    MERGE dbo.ProductAttributeOptions AS target
    USING (VALUES
        (@AttrId, 'GLOSS', N'Bóng kính (Polished/Glossy)', 1),
        (@AttrId, 'MATTE', N'Men mờ (Matte)', 2),
        (@AttrId, 'RUSTIC', N'Nhám định hình (Sugar/Rustic)', 3),
        (@AttrId, 'LAPATO', N'Bán bóng (Lapato)', 4)
    ) AS src (ProductAttributeID, OptionCode, OptionValue, DisplayOrder)
    ON target.ProductAttributeID = src.ProductAttributeID AND target.OptionCode = src.OptionCode
    WHEN MATCHED THEN UPDATE SET target.OptionValue = src.OptionValue, target.DisplayOrder = src.DisplayOrder, target.IsActive = 1
    WHEN NOT MATCHED THEN INSERT (ProductAttributeID, OptionCode, OptionValue, DisplayOrder, IsActive) VALUES (src.ProductAttributeID, src.OptionCode, src.OptionValue, src.DisplayOrder, 1);
END;

-- QUALITY_GRADE
SELECT @AttrId = ProductAttributeID FROM dbo.ProductAttributes WHERE AttributeCode = 'QUALITY_GRADE';
IF @AttrId IS NOT NULL
BEGIN
    MERGE dbo.ProductAttributeOptions AS target
    USING (VALUES
        (@AttrId, 'GRADE_1', N'Loại 1 (A1 - Chuẩn cao cấp)', 1),
        (@AttrId, 'GRADE_2', N'Loại 2 (A2 - Tiêu chuẩn)', 2),
        (@AttrId, 'GRADE_3', N'Loại 3 (A3 - Thứ phẩm)', 3)
    ) AS src (ProductAttributeID, OptionCode, OptionValue, DisplayOrder)
    ON target.ProductAttributeID = src.ProductAttributeID AND target.OptionCode = src.OptionCode
    WHEN MATCHED THEN UPDATE SET target.OptionValue = src.OptionValue, target.DisplayOrder = src.DisplayOrder, target.IsActive = 1
    WHEN NOT MATCHED THEN INSERT (ProductAttributeID, OptionCode, OptionValue, DisplayOrder, IsActive) VALUES (src.ProductAttributeID, src.OptionCode, src.OptionValue, src.DisplayOrder, 1);
END;

-- PAINT_TYPE
SELECT @AttrId = ProductAttributeID FROM dbo.ProductAttributes WHERE AttributeCode = 'PAINT_TYPE';
IF @AttrId IS NOT NULL
BEGIN
    MERGE dbo.ProductAttributeOptions AS target
    USING (VALUES
        (@AttrId, 'INTERIOR', N'Sơn nội thất', 1),
        (@AttrId, 'EXTERIOR', N'Sơn ngoại thất', 2),
        (@AttrId, 'PRIMER', N'Sơn lót kháng kiềm', 3),
        (@AttrId, 'WATERPROOF', N'Sơn chống thấm', 4),
        (@AttrId, 'OIL_EPOXY', N'Sơn dầu / Epoxy', 5)
    ) AS src (ProductAttributeID, OptionCode, OptionValue, DisplayOrder)
    ON target.ProductAttributeID = src.ProductAttributeID AND target.OptionCode = src.OptionCode
    WHEN MATCHED THEN UPDATE SET target.OptionValue = src.OptionValue, target.DisplayOrder = src.DisplayOrder, target.IsActive = 1
    WHEN NOT MATCHED THEN INSERT (ProductAttributeID, OptionCode, OptionValue, DisplayOrder, IsActive) VALUES (src.ProductAttributeID, src.OptionCode, src.OptionValue, src.DisplayOrder, 1);
END;

-- GLUE_TYPE
SELECT @AttrId = ProductAttributeID FROM dbo.ProductAttributes WHERE AttributeCode = 'GLUE_TYPE';
IF @AttrId IS NOT NULL
BEGIN
    MERGE dbo.ProductAttributeOptions AS target
    USING (VALUES
        (@AttrId, 'TILE_GLUE', N'Keo dán gạch đá', 1),
        (@AttrId, 'SILICONE', N'Keo Silicone xây dựng', 2),
        (@AttrId, 'WATERPROOF_MEMBRANE', N'Màng chống thấm bitum/PU', 3),
        (@AttrId, 'GROUT', N'Keo chà ron / miết mạch', 4)
    ) AS src (ProductAttributeID, OptionCode, OptionValue, DisplayOrder)
    ON target.ProductAttributeID = src.ProductAttributeID AND target.OptionCode = src.OptionCode
    WHEN MATCHED THEN UPDATE SET target.OptionValue = src.OptionValue, target.DisplayOrder = src.DisplayOrder, target.IsActive = 1
    WHEN NOT MATCHED THEN INSERT (ProductAttributeID, OptionCode, OptionValue, DisplayOrder, IsActive) VALUES (src.ProductAttributeID, src.OptionCode, src.OptionValue, src.DisplayOrder, 1);
END;

-- ADDITIVE_TYPE
SELECT @AttrId = ProductAttributeID FROM dbo.ProductAttributes WHERE AttributeCode = 'ADDITIVE_TYPE';
IF @AttrId IS NOT NULL
BEGIN
    MERGE dbo.ProductAttributeOptions AS target
    USING (VALUES
        (@AttrId, 'ACCELERATOR', N'Phụ gia đông kết nhanh', 1),
        (@AttrId, 'WATER_REDUCER', N'Phụ gia siêu dẻo / giảm nước', 2),
        (@AttrId, 'WATERPROOFING', N'Phụ gia chống thấm bê tông', 3),
        (@AttrId, 'CURING', N'Hợp chất bảo dưỡng bê tông', 4)
    ) AS src (ProductAttributeID, OptionCode, OptionValue, DisplayOrder)
    ON target.ProductAttributeID = src.ProductAttributeID AND target.OptionCode = src.OptionCode
    WHEN MATCHED THEN UPDATE SET target.OptionValue = src.OptionValue, target.DisplayOrder = src.DisplayOrder, target.IsActive = 1
    WHEN NOT MATCHED THEN INSERT (ProductAttributeID, OptionCode, OptionValue, DisplayOrder, IsActive) VALUES (src.ProductAttributeID, src.OptionCode, src.OptionValue, src.DisplayOrder, 1);
END;

-- PHYSICAL_FORM
SELECT @AttrId = ProductAttributeID FROM dbo.ProductAttributes WHERE AttributeCode = 'PHYSICAL_FORM';
IF @AttrId IS NOT NULL
BEGIN
    MERGE dbo.ProductAttributeOptions AS target
    USING (VALUES
        (@AttrId, 'LIQUID', N'Dung dịch lỏng', 1),
        (@AttrId, 'POWDER', N'Dạng bột khô', 2),
        (@AttrId, 'GEL', N'Dạng Gel / Nhũ tương', 3)
    ) AS src (ProductAttributeID, OptionCode, OptionValue, DisplayOrder)
    ON target.ProductAttributeID = src.ProductAttributeID AND target.OptionCode = src.OptionCode
    WHEN MATCHED THEN UPDATE SET target.OptionValue = src.OptionValue, target.DisplayOrder = src.DisplayOrder, target.IsActive = 1
    WHEN NOT MATCHED THEN INSERT (ProductAttributeID, OptionCode, OptionValue, DisplayOrder, IsActive) VALUES (src.ProductAttributeID, src.OptionCode, src.OptionValue, src.DisplayOrder, 1);
END;

-- PRESSURE_RATING
SELECT @AttrId = ProductAttributeID FROM dbo.ProductAttributes WHERE AttributeCode = 'PRESSURE_RATING';
IF @AttrId IS NOT NULL
BEGIN
    MERGE dbo.ProductAttributeOptions AS target
    USING (VALUES
        (@AttrId, 'PN6', N'PN6 (Áp lực 6 bar)', 1),
        (@AttrId, 'PN10', N'PN10 (Áp lực 10 bar)', 2),
        (@AttrId, 'PN16', N'PN16 (Áp lực 16 bar)', 3),
        (@AttrId, 'PN20', N'PN20 (Áp lực 20 bar)', 4),
        (@AttrId, 'PN25', N'PN25 (Áp lực 25 bar)', 5)
    ) AS src (ProductAttributeID, OptionCode, OptionValue, DisplayOrder)
    ON target.ProductAttributeID = src.ProductAttributeID AND target.OptionCode = src.OptionCode
    WHEN MATCHED THEN UPDATE SET target.OptionValue = src.OptionValue, target.DisplayOrder = src.DisplayOrder, target.IsActive = 1
    WHEN NOT MATCHED THEN INSERT (ProductAttributeID, OptionCode, OptionValue, DisplayOrder, IsActive) VALUES (src.ProductAttributeID, src.OptionCode, src.OptionValue, src.DisplayOrder, 1);
END;

-- VOLTAGE
SELECT @AttrId = ProductAttributeID FROM dbo.ProductAttributes WHERE AttributeCode = 'VOLTAGE';
IF @AttrId IS NOT NULL
BEGIN
    MERGE dbo.ProductAttributeOptions AS target
    USING (VALUES
        (@AttrId, '220V', N'220V (1 Pha)', 1),
        (@AttrId, '380V', N'380V (3 Pha)', 2),
        (@AttrId, '12V_24V', N'12V / 24V (Một chiều DC)', 3)
    ) AS src (ProductAttributeID, OptionCode, OptionValue, DisplayOrder)
    ON target.ProductAttributeID = src.ProductAttributeID AND target.OptionCode = src.OptionCode
    WHEN MATCHED THEN UPDATE SET target.OptionValue = src.OptionValue, target.DisplayOrder = src.DisplayOrder, target.IsActive = 1
    WHEN NOT MATCHED THEN INSERT (ProductAttributeID, OptionCode, OptionValue, DisplayOrder, IsActive) VALUES (src.ProductAttributeID, src.OptionCode, src.OptionValue, src.DisplayOrder, 1);
END;

-- WOOD_TYPE
SELECT @AttrId = ProductAttributeID FROM dbo.ProductAttributes WHERE AttributeCode = 'WOOD_TYPE';
IF @AttrId IS NOT NULL
BEGIN
    MERGE dbo.ProductAttributeOptions AS target
    USING (VALUES
        (@AttrId, 'MDF', N'Ván sợi MDF', 1),
        (@AttrId, 'HDF', N'Ván sợi mật độ cao HDF', 2),
        (@AttrId, 'PLYWOOD', N'Ván ép Plywood / Gỗ dán', 3),
        (@AttrId, 'SOLID_PINE', N'Gỗ thông thanh ghép', 4),
        (@AttrId, 'PARTICLE_BOARD', N'Ván dăm MFC', 5)
    ) AS src (ProductAttributeID, OptionCode, OptionValue, DisplayOrder)
    ON target.ProductAttributeID = src.ProductAttributeID AND target.OptionCode = src.OptionCode
    WHEN MATCHED THEN UPDATE SET target.OptionValue = src.OptionValue, target.DisplayOrder = src.DisplayOrder, target.IsActive = 1
    WHEN NOT MATCHED THEN INSERT (ProductAttributeID, OptionCode, OptionValue, DisplayOrder, IsActive) VALUES (src.ProductAttributeID, src.OptionCode, src.OptionValue, src.DisplayOrder, 1);
END;

-- MOISTURE_RESISTANCE
SELECT @AttrId = ProductAttributeID FROM dbo.ProductAttributes WHERE AttributeCode = 'MOISTURE_RESISTANCE';
IF @AttrId IS NOT NULL
BEGIN
    MERGE dbo.ProductAttributeOptions AS target
    USING (VALUES
        (@AttrId, 'STANDARD', N'Tiêu chuẩn thường', 1),
        (@AttrId, 'MR_GREEN', N'Chống ẩm lõi xanh (HMR)', 2),
        (@AttrId, 'WATERPROOF', N'Chống nước hoàn toàn', 3)
    ) AS src (ProductAttributeID, OptionCode, OptionValue, DisplayOrder)
    ON target.ProductAttributeID = src.ProductAttributeID AND target.OptionCode = src.OptionCode
    WHEN MATCHED THEN UPDATE SET target.OptionValue = src.OptionValue, target.DisplayOrder = src.DisplayOrder, target.IsActive = 1
    WHEN NOT MATCHED THEN INSERT (ProductAttributeID, OptionCode, OptionValue, DisplayOrder, IsActive) VALUES (src.ProductAttributeID, src.OptionCode, src.OptionValue, src.DisplayOrder, 1);
END;

-- GLASS_TYPE
SELECT @AttrId = ProductAttributeID FROM dbo.ProductAttributes WHERE AttributeCode = 'GLASS_TYPE';
IF @AttrId IS NOT NULL
BEGIN
    MERGE dbo.ProductAttributeOptions AS target
    USING (VALUES
        (@AttrId, 'TEMPERED_8', N'Kính cường lực 8mm', 1),
        (@AttrId, 'TEMPERED_10', N'Kính cường lực 10mm', 2),
        (@AttrId, 'TEMPERED_12', N'Kính cường lực 12mm', 3),
        (@AttrId, 'LAMINATED', N'Kính dán an toàn 2 lớp', 4),
        (@AttrId, 'DOOR_ALU', N'Cửa khung nhôm kính Xingfa', 5),
        (@AttrId, 'DOOR_PLASTIC', N'Cửa nhựa lõi thép / Composite', 6)
    ) AS src (ProductAttributeID, OptionCode, OptionValue, DisplayOrder)
    ON target.ProductAttributeID = src.ProductAttributeID AND target.OptionCode = src.OptionCode
    WHEN MATCHED THEN UPDATE SET target.OptionValue = src.OptionValue, target.DisplayOrder = src.DisplayOrder, target.IsActive = 1
    WHEN NOT MATCHED THEN INSERT (ProductAttributeID, OptionCode, OptionValue, DisplayOrder, IsActive) VALUES (src.ProductAttributeID, src.OptionCode, src.OptionValue, src.DisplayOrder, 1);
END;

-- PLATING
SELECT @AttrId = ProductAttributeID FROM dbo.ProductAttributes WHERE AttributeCode = 'PLATING';
IF @AttrId IS NOT NULL
BEGIN
    MERGE dbo.ProductAttributeOptions AS target
    USING (VALUES
        (@AttrId, 'ELECTRO_ZINC', N'Mạ kẽm điện phân (Trắng/Bảy màu)', 1),
        (@AttrId, 'HOT_DIP_ZINC', N'Mạ kẽm nhúng nóng', 2),
        (@AttrId, 'INOX_304', N'Thép không gỉ Inox 304', 3),
        (@AttrId, 'INOX_201', N'Thép không gỉ Inox 201', 4),
        (@AttrId, 'BLACK_OXIDE', N'Nhuộm đen / Thép đen', 5)
    ) AS src (ProductAttributeID, OptionCode, OptionValue, DisplayOrder)
    ON target.ProductAttributeID = src.ProductAttributeID AND target.OptionCode = src.OptionCode
    WHEN MATCHED THEN UPDATE SET target.OptionValue = src.OptionValue, target.DisplayOrder = src.DisplayOrder, target.IsActive = 1
    WHEN NOT MATCHED THEN INSERT (ProductAttributeID, OptionCode, OptionValue, DisplayOrder, IsActive) VALUES (src.ProductAttributeID, src.OptionCode, src.OptionValue, src.DisplayOrder, 1);
END;
GO

-- ==============================================================================
-- BƯỚC 3: CẤU HÌNH LIÊN KẾT NHÓM HÀNG VÀ THUỘC TÍNH (ProductGroupAttributes)
-- ==============================================================================
PRINT N'>>> BƯỚC 3: NẠP CẤU HÌNH LIÊN KẾT THUỘC TÍNH THEO 15 NHÓM...';

CREATE TABLE #GroupAttrMap
(
    GroupCode VARCHAR(50),
    AttributeCode VARCHAR(50),
    IsRequired BIT,
    DisplayOrder INT,
    DefaultValue NVARCHAR(1000)
);

INSERT INTO #GroupAttrMap VALUES
-- PG01: Xi măng
('PG01', 'BRAND', 1, 1, NULL),
('PG01', 'CEMENT_TYPE', 1, 2, N'PCB40'),
('PG01', 'BAG_WEIGHT', 1, 3, N'50'),
('PG01', 'CEMENT_GRADE', 1, 4, N'40MPA'),

-- PG02: Thép xây dựng
('PG02', 'BRAND', 1, 1, NULL),
('PG02', 'DIAMETER', 1, 2, NULL),
('PG02', 'LENGTH', 1, 3, N'11.7'),
('PG02', 'STEEL_STANDARD', 1, 4, N'TCVN1651'),
('PG02', 'STEEL_GRADE', 1, 5, N'CB300V'),

-- PG03: Tôn và vật liệu lợp
('PG03', 'BRAND', 1, 1, NULL),
('PG03', 'THICKNESS', 1, 2, N'0.45'),
('PG03', 'WIDTH', 1, 3, N'1.08'),
('PG03', 'LENGTH', 1, 4, N'6.0'),
('PG03', 'COLOR', 1, 5, N'Xanh ngọc'),
('PG03', 'COATING', 0, 6, N'Mạ lạnh AZ150'),

-- PG04: Gạch xây và gạch ốp lát
('PG04', 'BRAND', 1, 1, NULL),
('PG04', 'DIMENSION', 1, 2, N'600x600'),
('PG04', 'COLOR', 0, 3, NULL),
('PG04', 'SURFACE_TYPE', 1, 4, N'GLOSS'),
('PG04', 'QUALITY_GRADE', 1, 5, N'GRADE_1'),

-- PG05: Cát, đá và vật liệu rời
('PG05', 'MATERIAL_TYPE', 1, 1, N'Cát vàng xây tô'),
('PG05', 'GRAIN_SIZE', 1, 2, N'1.5 - 2.0'),
('PG05', 'SOURCE_LOCATION', 0, 3, N'Mỏ cát Sông Lô'),

-- PG06: Sơn và chất phủ
('PG06', 'BRAND', 1, 1, N'Dulux'),
('PG06', 'COLOR', 1, 2, N'Trắng sứ'),
('PG06', 'VOLUME', 1, 3, N'18'),
('PG06', 'PAINT_TYPE', 1, 4, N'INTERIOR'),
('PG06', 'APPLICATION_SURFACE', 0, 5, N'Tường trát vữa, bê tông nội thất'),

-- PG07: Keo và vật liệu chống thấm
('PG07', 'BRAND', 1, 1, N'Sika'),
('PG07', 'GLUE_TYPE', 1, 2, N'TILE_GLUE'),
('PG07', 'WEIGHT_NET', 1, 3, N'25'),
('PG07', 'COLOR', 0, 4, N'Xám'),
('PG07', 'USAGE_SCOPE', 0, 5, N'Dán gạch hồ bơi & ngoài trời'),

-- PG08: Phụ gia bê tông và hóa chất
('PG08', 'BRAND', 1, 1, N'Sika'),
('PG08', 'ADDITIVE_TYPE', 1, 2, N'WATERPROOFING'),
('PG08', 'PHYSICAL_FORM', 1, 3, N'LIQUID'),
('PG08', 'VOLUME', 0, 4, N'25'),
('PG08', 'USAGE_RATIO', 0, 5, N'1 - 2% theo khối lượng xi măng'),

-- PG09: Ống và phụ kiện cấp thoát nước
('PG09', 'BRAND', 1, 1, N'Tiền Phong'),
('PG09', 'DIAMETER', 1, 2, N'110'),
('PG09', 'LENGTH', 1, 3, N'4'),
('PG09', 'THICKNESS', 0, 4, N'3.2'),
('PG09', 'PRESSURE_RATING', 1, 5, N'PN10'),
('PG09', 'MATERIAL', 1, 6, N'uPVC'),

-- PG10: Thiết bị điện
('PG10', 'BRAND', 1, 1, N'Cadivi'),
('PG10', 'VOLTAGE', 1, 2, N'220V'),
('PG10', 'CORE_COUNT', 1, 3, N'2'),
('PG10', 'CROSS_SECTION', 1, 4, N'2.5'),
('PG10', 'CURRENT_RATING', 0, 5, N'16'),

-- PG11: Thiết bị vệ sinh
('PG11', 'BRAND', 1, 1, N'Toto'),
('PG11', 'MATERIAL', 1, 2, N'Sứ tráng men CeFiONtect'),
('PG11', 'COLOR', 1, 3, N'Trắng tinh'),
('PG11', 'DIMENSION', 0, 4, N'700x380x750'),

-- PG12: Gỗ, ván và vật liệu nội thất
('PG12', 'BRAND', 1, 1, N'An Cường'),
('PG12', 'WOOD_TYPE', 1, 2, N'MDF'),
('PG12', 'THICKNESS', 1, 3, N'17'),
('PG12', 'DIMENSION', 1, 4, N'1220x2440'),
('PG12', 'MOISTURE_RESISTANCE', 1, 5, N'MR_GREEN'),

-- PG13: Kính và cửa
('PG13', 'BRAND', 0, 1, NULL),
('PG13', 'THICKNESS', 1, 2, N'10'),
('PG13', 'DIMENSION', 1, 3, N'2100x900'),
('PG13', 'COLOR', 0, 4, N'Trong suốt'),
('PG13', 'GLASS_TYPE', 1, 5, N'TEMPERED_10'),

-- PG14: Ngũ kim và phụ kiện xây dựng
('PG14', 'DIMENSION', 1, 1, N'M8x50'),
('PG14', 'MATERIAL', 1, 2, N'Thép carbon'),
('PG14', 'THREAD_TYPE', 0, 3, N'Ren lửng'),
('PG14', 'PLATING', 1, 4, N'ELECTRO_ZINC'),

-- PG15: Dụng cụ và thiết bị thi công
('PG15', 'BRAND', 1, 1, N'Bosch'),
('PG15', 'MODEL', 1, 2, N'GBH 2-26 DRE'),
('PG15', 'POWER', 0, 3, N'800'),
('PG15', 'WARRANTY_MONTHS', 1, 4, N'12');

-- THỰC THI GÁN VÀO BẢNG ProductGroupAttributes
MERGE dbo.ProductGroupAttributes AS target
USING (
    SELECT 
        pg.ProductGroupID,
        pa.ProductAttributeID,
        m.IsRequired,
        m.DisplayOrder,
        m.DefaultValue
    FROM #GroupAttrMap m
    JOIN dbo.ProductGroups pg ON pg.GroupCode = m.GroupCode
    JOIN dbo.ProductAttributes pa ON pa.AttributeCode = m.AttributeCode
) AS src
ON target.ProductGroupID = src.ProductGroupID AND target.ProductAttributeID = src.ProductAttributeID
WHEN MATCHED THEN
    UPDATE SET 
        target.IsRequired = src.IsRequired,
        target.DisplayOrder = src.DisplayOrder,
        target.DefaultValue = src.DefaultValue
WHEN NOT MATCHED THEN
    INSERT (ProductGroupID, ProductAttributeID, IsRequired, DisplayOrder, DefaultValue)
    VALUES (src.ProductGroupID, src.ProductAttributeID, src.IsRequired, src.DisplayOrder, src.DefaultValue);

DROP TABLE #GroupAttrMap;

PRINT N'======================================================================';
PRINT N'>>> HOÀN TẤT DỌN DẸP & ĐỒNG BỘ 15 NHÓM VÀ THUỘC TÍNH THÀNH CÔNG!';
PRINT N'======================================================================';
GO
