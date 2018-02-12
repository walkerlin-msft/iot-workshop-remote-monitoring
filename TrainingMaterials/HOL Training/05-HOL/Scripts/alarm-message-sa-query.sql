WITH StreamAvgData AS 
(
    SELECT
        deviceId,
        AVG(CAST(speed AS BIGINT)) AS AvgSpeed,
        AVG(CAST(depreciation AS float)) AS AvgDepreciation,
        System.TimeStamp AS LocalTime,
        System.TimeStamp AS CreatedAt
    FROM [iothub] TIMESTAMP BY time
    GROUP BY
        deviceId,
    TumblingWindow(second, 30)
),
AlarmData AS
(
    SELECT
        Stream.deviceId AS IoTHubDeviceID,
        'CutOutSpeed MessageID' AS [MessageID],
        'CutOutSpeed' as [AlarmType],
        Stream.AvgSpeed as [Reading],
        Ref.[CutOutSpeed] as [Threshold],
        Stream.LocalTime,
        Stream.CreatedAt
    FROM [StreamAvgData] Stream
    JOIN [devicerules] Ref 
    ON 
        Stream.deviceId = Ref.[DeviceID]
    WHERE Ref.[CutOutSpeed] IS NOT null AND Stream.AvgSpeed > Ref.[CutOutSpeed]

    UNION ALL

    SELECT
        Stream2.deviceId AS IoTHubDeviceID,
        'Repair MessageID' AS [MessageID],
        'Repair' as [AlarmType],
        Stream2.AvgDepreciation as [Reading],
        Ref2.[Repair] as [Threshold],
        Stream2.LocalTime,
        Stream2.CreatedAt
    FROM [StreamAvgData] Stream2
    JOIN [devicerules] Ref2 
    ON 
        Stream2.deviceId = Ref2.[DeviceID]
    WHERE Ref2.[Repair] IS NOT null AND Stream2.AvgDepreciation < Ref2.[Repair]    
)

SELECT * INTO [alarmsb] FROM AlarmData
