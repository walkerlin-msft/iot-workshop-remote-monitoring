WITH AlarmData AS 
(
	SELECT
		Stream.IoTHub.[ConnectionDeviceId] AS [IoTHubDeviceID],
		Stream.[msgId] AS [MessageID],
		'CutOutSpeed' as [AlarmType],
		Stream.[speed] as [Reading],
		Ref.[CutOutSpeed] as [Threshold],
		Stream.[time] AS [LocalTime],
		Stream.[EventEnqueuedUtcTime] AS [CreatedAt]
	FROM [iothub] Stream
	JOIN [devicerules] Ref 
	ON 
		Stream.IoTHub.[ConnectionDeviceId] = Ref.[DeviceID]
	WHERE Ref.[CutOutSpeed] IS NOT null AND Stream.[speed] > Ref.[CutOutSpeed]

	UNION ALL

	SELECT
		Stream2.IoTHub.[ConnectionDeviceId] AS [IoTHubDeviceID],
		Stream2.[msgId] AS [MessageID],
		'Repair' as [AlarmType],
		Stream2.[depreciation] as [Reading],
		Ref2.[Repair] as [Threshold],
		Stream2.[time] AS [LocalTime],
		Stream2.[EventEnqueuedUtcTime] AS [CreatedAt]
	FROM [iothub] Stream2
	JOIN [devicerules] Ref2 
	ON 
		Stream2.IoTHub.[ConnectionDeviceId] = Ref2.[DeviceID]
	WHERE Ref2.[Repair] IS NOT null AND Stream2.[depreciation] < Ref2.[Repair]
)

SELECT * INTO [alarmsb] FROM AlarmData