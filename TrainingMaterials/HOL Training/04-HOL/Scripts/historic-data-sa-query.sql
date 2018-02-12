WITH HistoricData AS (
	SELECT
		Stream.IoTHub.[ConnectionDeviceId] AS [IoTHubDeviceID],
		Stream.[msgId] AS [MessageID],
		Stream.[speed] AS [WindSpeed],
		Stream.[depreciation] AS [Depreciation],
		Stream.[power] AS [Power],
		Ref.[Altitude] AS [Altitude],
		Ref.[Longitude] AS [Longitude],
		Ref.[Latitude] AS [Latitude],
		Ref.[CutOutSpeed] AS [CutOutSpeed],
		CASE
			WHEN Stream.[speed] > Ref.[CutOutSpeed] THEN 1
			ELSE 0
		END AS [CutOutSpeedAlarm],
		Ref.[Repair] AS [Repair],
		CASE
			WHEN Stream.[depreciation] < Ref.[Repair] THEN 1
			ELSE 0
		END AS [RepairAlarm],
		Stream.[time] AS [LocalTime],
		Stream.[EventEnqueuedUtcTime] AS [CreatedAt]
	FROM 
		[iothub] Stream
	JOIN [devicerules] Ref 
	ON 
		Stream.IoTHub.[ConnectionDeviceId] = Ref.[DeviceID]
)

SELECT * INTO [blob] FROM HistoricData
SELECT * INTO [sqldb] FROM HistoricData