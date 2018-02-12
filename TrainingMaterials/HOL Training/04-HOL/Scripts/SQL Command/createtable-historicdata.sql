CREATE SCHEMA [Prod]
CREATE TABLE [Prod].[HistoricData]
(
    [Id] INT IDENTITY (1, 1) NOT NULL,
    [IoTHubDeviceID] NVARCHAR(128) NULL,
    [MessageID] NVARCHAR(128) NULL,
    [WindSpeed] FLOAT(10) NULL,
    [Depreciation] FLOAT(10) NULL,
    [Power] FLOAT(10) NULL,
    [Altitude] Decimal(9,6) NULL,
    [Longitude] Decimal(9,6) NULL,
    [Latitude] Decimal(9,6) NULL,
    [CutOutSpeed] FLOAT NULL,
    [CutOutSpeedAlarm] INT DEFAULT ((0)) NULL,
    [Repair] FLOAT NULL,
    [RepairAlarm] INT DEFAULT ((0)) NULL,
    [LocalTime] DATETIME DEFAULT (getdate()) NULL,
    [CreatedAt] DATETIME DEFAULT (getdate()) NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC)
)