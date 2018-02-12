// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <stdlib.h>

#include <stdio.h>
#include <stdint.h>
#include <unistd.h>

#include <time.h>

#include "serializer.h"
#include "azure_c_shared_utility/threadapi.h"
#include "azure_c_shared_utility/sastoken.h"
#include "azure_c_shared_utility/platform.h"
#include "iothub_client.h"
#include "iothubtransportamqp.h"
#include "iothub_client_ll.h"

#ifdef MBED_BUILD_TIMESTAMP
#include "certs.h"
#endif // MBED_BUILD_TIMESTAMP

/*String containing Hostname, Device Id & Device Key in the format:             */
/*  "HostName=<host_name>;DeviceId=<device_id>;SharedAccessKey=<device_key>"    */
static const char* connectionString = "[device connection string]";

static const double MAXIMUM_DEPRECIATION = 1.0f;
static const double MINIMUM_DEPRECIATION = 0.3f;
static const double DEPRECIATION_RATE = 0.01f;
static double _resetDepreciationValue = 1.0f; // 100%
static int _depreciationCount = 0;
static int _isStopped = 0;

// Define the Model
BEGIN_NAMESPACE(WeatherStation);

DECLARE_MODEL(ContosoAnemometer,
/* Add new data*/
WITH_DATA(ascii_char_ptr, deviceId),
WITH_DATA(ascii_char_ptr, msgId),
WITH_DATA(int, speed),
WITH_DATA(double, depreciation),
WITH_DATA(double, power),
WITH_DATA(ascii_char_ptr, time),
WITH_ACTION(CutoutSpeedWarning),
WITH_ACTION(RepairWarning),
WITH_ACTION(TurnOnOff, int, On),
WITH_ACTION(ResetDepreciation, double, Depreciation)
);

END_NAMESPACE(WeatherStation);


EXECUTE_COMMAND_RESULT CutoutSpeedWarning(ContosoAnemometer* device)
{
    (void)device;
    (void)printf("\r\n *****C2D***** Cutout Speed Warning.\r\n\r\n");
    return EXECUTE_COMMAND_SUCCESS;
}

EXECUTE_COMMAND_RESULT RepairWarning(ContosoAnemometer* device)
{
    (void)device;
    (void)printf("\r\n *****C2D***** Repair Warning.\r\n\r\n");
    return EXECUTE_COMMAND_SUCCESS;
}

EXECUTE_COMMAND_RESULT TurnOnOff(ContosoAnemometer* device, int On)
{
    (void)device;    
	
	if(On == 0)
		_isStopped = 1;
	else
		_isStopped = 0;
	
	(void)printf("\r\n *****C2D***** Turn OnOff, _isStopped = %d.\r\n\r\n", _isStopped);
	
    return EXECUTE_COMMAND_SUCCESS;
}

EXECUTE_COMMAND_RESULT ResetDepreciation(ContosoAnemometer* device, double Depreciation)
{
	(void)device;
	
	_resetDepreciationValue = Depreciation;
    _depreciationCount = 0;
						
	(void)printf("\r\n *****C2D***** Reset the Depreciation to %f.\r\n\r\n", _resetDepreciationValue);
	return EXECUTE_COMMAND_SUCCESS;
}

void sendCallback(IOTHUB_CLIENT_CONFIRMATION_RESULT result, void* userContextCallback)
{
    unsigned int messageTrackingId = (unsigned int)(uintptr_t)userContextCallback;

    (void)printf("Message Id: %u Received.\r\n", messageTrackingId);

    (void)printf("Result Call Back Called! Result is: %s \r\n", ENUM_TO_STRING(IOTHUB_CLIENT_CONFIRMATION_RESULT, result));
}


static void sendMessage(IOTHUB_CLIENT_HANDLE iotHubClientHandle, const unsigned char* buffer, size_t size)
{
    static unsigned int messageTrackingId;
    IOTHUB_MESSAGE_HANDLE messageHandle = IoTHubMessage_CreateFromByteArray(buffer, size);
    if (messageHandle == NULL)
    {
        printf("unable to create a new IoTHubMessage\r\n");
    }
    else
    {
		MAP_HANDLE propMap = IoTHubMessage_Properties(messageHandle);		
		Map_AddOrUpdate(propMap, "msgType", "Telemetry");

        if (IoTHubClient_SendEventAsync(iotHubClientHandle, messageHandle, sendCallback, (void*)(uintptr_t)messageTrackingId) != IOTHUB_CLIENT_OK)
        {
            printf("failed to hand over the message to IoTHubClient");
        }
        else
        {
            printf("IoTHubClient accepted the message for delivery\r\n");
        }

        IoTHubMessage_Destroy(messageHandle);
    }
    free((void*)buffer);
    messageTrackingId++;
}

static IOTHUBMESSAGE_DISPOSITION_RESULT IoTHubMessage(IOTHUB_MESSAGE_HANDLE message, void* userContextCallback)
{
    IOTHUBMESSAGE_DISPOSITION_RESULT result;
    const unsigned char* buffer;
    size_t size;
    if (IoTHubMessage_GetByteArray(message, &buffer, &size) != IOTHUB_MESSAGE_OK)
    {
        printf("unable to IoTHubMessage_GetByteArray\r\n");
        result = EXECUTE_COMMAND_ERROR;
    }
    else
    {
        /*buffer is not zero terminated*/
        char* temp = malloc(size + 1);
        if (temp == NULL)
        {
            printf("failed to malloc\r\n");
            result = EXECUTE_COMMAND_ERROR;
        }
        else
        {
            memcpy(temp, buffer, size);
            temp[size] = '\0';
            EXECUTE_COMMAND_RESULT executeCommandResult = EXECUTE_COMMAND(userContextCallback, temp);
            result =
                (executeCommandResult == EXECUTE_COMMAND_ERROR) ? IOTHUBMESSAGE_ABANDONED :
                (executeCommandResult == EXECUTE_COMMAND_SUCCESS) ? IOTHUBMESSAGE_ACCEPTED :
                IOTHUBMESSAGE_REJECTED;
            free(temp);
        }
    }
    return result;
}

/* Simulate the real wind power */
static double getNewDepreciation(int i)
{
	if (i % 5 == 0)
		_depreciationCount++;

	double depreciation = _resetDepreciationValue - (_depreciationCount * DEPRECIATION_RATE);

	if (depreciation < MINIMUM_DEPRECIATION)
		depreciation = MINIMUM_DEPRECIATION;

	return depreciation;
}
		
static double getWindPower(int speed, double depreciation)
{
	if (speed <= 3)
		return 0;
	else if (speed <= 7)
		return (speed - 3) * 50 * depreciation;
	else if (speed <= 9)
		return (speed - 7) * 100 * depreciation + 200;
	else if (speed < 12)
		return (speed - 9) * 200 * depreciation + 400;
	else
		return 1000 * depreciation;
}

static void getUTCtime(char *buf, size_t max) {
	time_t now;
	time(&now);	
	strftime(buf, max, "%FT%TZ", gmtime(&now));
	//printf("getUTCtime buf=%s\r\n", buf);
	return;
}

void simplesample_amqp_run(void)
{
    if (platform_init() != 0)
    {
        printf("Failed to initialize the platform.\r\n");
    }
    else
    {
        if (serializer_init(NULL) != SERIALIZER_OK)
        {
            (void)printf("Failed on serializer_init\r\n");
        }
        else
        {
            /* Setup IoTHub client configuration */
            IOTHUB_CLIENT_HANDLE iotHubClientHandle = IoTHubClient_CreateFromConnectionString(connectionString, AMQP_Protocol);
            srand((unsigned int)time(NULL));			

            // Turn on Log 
            bool trace = true;
            (void)IoTHubClient_SetOption(iotHubClientHandle, "logtrace", &trace);

            if (iotHubClientHandle == NULL)
            {
                (void)printf("Failed on IoTHubClient_Create\r\n");
            }
            else
            {
#ifdef MBED_BUILD_TIMESTAMP
                // For mbed add the certificate information
                if (IoTHubClient_SetOption(iotHubClientHandle, "TrustedCerts", certificates) != IOTHUB_CLIENT_OK)
                {
                    (void)printf("failure to set option \"TrustedCerts\"\r\n");
                }
#endif // MBED_BUILD_TIMESTAMP

                ContosoAnemometer* myWeather = CREATE_MODEL_INSTANCE(WeatherStation, ContosoAnemometer);
                if (myWeather == NULL)
                {
                    (void)printf("Failed on CREATE_MODEL_INSTANCE\r\n");
                }
                else
                {
                    unsigned char* destination;
                    size_t destinationSize;

                    if (IoTHubClient_SetMessageCallback(iotHubClientHandle, IoTHubMessage, myWeather) != IOTHUB_CLIENT_OK)
                    {
                        printf("unable to IoTHubClient_SetMessageCallback\r\n");
                    }
                    else
                    {                        
                        myWeather->deviceId = "LinuxTurbine";
						int minWindSpeed = 2; // m/s
						char messageId[30];
						int mesgCnt = 1;	
						char utc_time[sizeof "2011-10-08T07:07:09Z"];						
						
						while (mesgCnt > 0) 
						{
							if(_isStopped == 0) {
							
								sprintf(messageId, "message id %d", mesgCnt);
								myWeather->msgId = messageId;
								
								int speed = minWindSpeed + (rand() % 19); // 2~20	
								myWeather->speed = 	speed;
								
								double depreciation = getNewDepreciation(mesgCnt);
								myWeather->depreciation = depreciation;
								
								myWeather->power = getWindPower(speed, depreciation);
								
								getUTCtime(utc_time, sizeof utc_time);
								myWeather->time = utc_time;// "2016-11-23T05:14:19.649Z";

								if (SERIALIZE(&destination, &destinationSize,
									myWeather->deviceId, myWeather->msgId,
									myWeather->speed, myWeather->depreciation,
									myWeather->power, myWeather->time) != CODEFIRST_OK)
								{
									(void)printf("Failed to serialize\r\n");
									break;
								}
								else
								{
									sendMessage(iotHubClientHandle, destination, destinationSize);
								}
								mesgCnt++;
							}	
							
							sleep(5);// 5 seconds							
						}
                    }
                    DESTROY_MODEL_INSTANCE(myWeather);
                }
                IoTHubClient_Destroy(iotHubClientHandle);
            }
            serializer_deinit();
        }
        platform_deinit();
    }
}