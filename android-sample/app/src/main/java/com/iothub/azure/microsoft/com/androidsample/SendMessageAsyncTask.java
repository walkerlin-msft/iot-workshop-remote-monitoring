package com.iothub.azure.microsoft.com.androidsample;

import android.os.AsyncTask;
import android.util.Log;

import com.microsoft.azure.sdk.iot.device.DeviceClient;
import com.microsoft.azure.sdk.iot.device.IotHubClientProtocol;
import com.microsoft.azure.sdk.iot.device.IotHubEventCallback;
import com.microsoft.azure.sdk.iot.device.IotHubMessageResult;
import com.microsoft.azure.sdk.iot.device.IotHubStatusCode;
import com.microsoft.azure.sdk.iot.device.Message;
import com.microsoft.azure.sdk.iot.device.MessageCallback;

import org.json.JSONObject;

import java.io.IOException;
import java.net.URISyntaxException;
import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.Date;
import java.util.TimeZone;

/**
 * Created by a-walin on 2016/12/14.
 */

public class SendMessageAsyncTask extends AsyncTask<Void, ArrayList<String>, Void> {

    private static final String TAG = "SendMessageAsyncTask";

    MainActivity activity;
    ProgressUpdateCallback progressUpdateCallback;
    double windPower;

    public SendMessageAsyncTask(MainActivity activity, ProgressUpdateCallback progressUpdateCallback) {
        this.activity = activity;
        this.progressUpdateCallback = progressUpdateCallback;
    }

    private DeviceClient createDeviceClient() {

        try {
            DeviceClient client = new DeviceClient(MainActivity.connString, MainActivity.protocol);

            return client;
        } catch (URISyntaxException e) {
            Log.e(TAG, "URISyntaxException e=" + e.getMessage());
        } catch (Exception e) {
            Log.e(TAG, "Exception e=" + e.getMessage());
        }

        return null;
    }

    private DeviceClient createReceiveDeviceClient() {
        try {
            DeviceClient client = new DeviceClient(MainActivity.connString, MainActivity.protocol);

            if (MainActivity.protocol == IotHubClientProtocol.MQTT) {
                MessageCallbackMqtt callback = new MessageCallbackMqtt();
                Counter counter = new Counter(0);
                client.setMessageCallback(callback, counter);
            } else {
                MessageCallbackHttp callback = new MessageCallbackHttp();
                Counter counter = new Counter(0);
                client.setMessageCallback(callback, counter);
            }

            return client;
        } catch (URISyntaxException e) {
            Log.e(TAG, "URISyntaxException e=" + e.getMessage());
        } catch (Exception e) {
            Log.e(TAG, "Exception e=" + e.getMessage());
        }

        return null;
    }

    @Override
    protected Void doInBackground(Void... longs) {

        DeviceClient client = createDeviceClient();
        DeviceClient receiveClient = createReceiveDeviceClient();

        try {
            client.open();
            receiveClient.open();

            int i = 1;
            while (i > 0) {
                if (isCancelled()) {
                    client.close();
                    receiveClient.close();
                    return null;
                }

                JSONObject json = new JSONObject();
                json.put("deviceId", "LinuxTurbine");
                json.put("msgId", "Message id " + Integer.toString(i));
                json.put("speed", this.activity.getSpeed());
                json.put("depreciation", this.activity.getDepreciation());
                json.put("power", getPower(this.activity.getSpeed(), this.activity.getDepreciation()));
                json.put("time", getUTCdatetimeAsString());
                Log.d(TAG, json.toString());
                try {
                    Message msg = new Message(json.toString());
                    msg.setProperty("msgType", "Telemetry");
                    msg.setMessageId(java.util.UUID.randomUUID().toString());

                    EventCallback eventCallback = new EventCallback();
                    client.sendEventAsync(msg, eventCallback, i);

                    publishMessage("SEND", json.toString());
                } catch (Exception e) {
                }
                try {
                    Thread.sleep(2000);
                } catch (InterruptedException e) {
                    e.printStackTrace();
                }

                i++;
            }
        } catch (IOException e) {
            Log.e(TAG, "IOException e=" + e.getMessage());
        } catch (Exception e) {
            Log.e(TAG, "Exception e=" + e.getMessage());
        }

        return null;
    }

    @Override
    protected void onProgressUpdate(ArrayList<String>... values) {
        super.onProgressUpdate(values);

        if (this.progressUpdateCallback != null)
            this.progressUpdateCallback.callback(values[0]);
    }

    protected class EventCallback implements IotHubEventCallback {
        public void execute(IotHubStatusCode status, Object context) {
            Integer i = (Integer) context;
            Log.d(TAG, "IoT Hub responded to message " + i.toString()
                    + " with status " + status.name());
        }
    }

    // Our MQTT doesn't support abandon/reject, so we will only display the messaged received
// from IoTHub and return COMPLETE
    class MessageCallbackMqtt implements com.microsoft.azure.sdk.iot.device.MessageCallback {
        public IotHubMessageResult execute(Message msg, Object context) {
            Counter counter = (Counter) context;
            String content = new String(msg.getBytes(), Message.DEFAULT_IOTHUB_MESSAGE_CHARSET);
            String mesg = "Received message " + counter.toString()
                    + " with content: " + content;
            Log.d(TAG, "MessageCallbackMqtt " + mesg);

            publishMessage("RECEIVE", content);

            counter.increment();
            return IotHubMessageResult.COMPLETE;
        }
    }

    protected class MessageCallbackHttp implements com.microsoft.azure.sdk.iot.device.MessageCallback {
        public IotHubMessageResult execute(Message msg, Object context) {
            Counter counter = (Counter) context;
            String content = new String(msg.getBytes(), Message.DEFAULT_IOTHUB_MESSAGE_CHARSET);
            String mesg = "Received message " + counter.toString()
                    + " with content: " + content;
            Log.d(TAG, "MessageCallbackHttp " + mesg);

            int switchVal = 0;//counter.get() % 3;
            IotHubMessageResult res;
            switch (switchVal) {
                case 0:
                    res = IotHubMessageResult.COMPLETE;
                    break;
                case 1:
                    res = IotHubMessageResult.ABANDON;
                    break;
                case 2:
                    res = IotHubMessageResult.REJECT;
                    break;
                default:
                    // should never happen.
                    throw new IllegalStateException("Invalid message result specified.");
            }

            Log.d(TAG, "Responding to message " + counter.toString() + " with " + res.name());

            publishMessage("RECEIVE", content + " with " + res.name());

            counter.increment();

            return res;
        }
    }

    private void publishMessage(String key, String value) {
        ArrayList<String> values = new ArrayList<String>();
        values.add(key);
        value += "["+getUTCdatetimeAsString()+"]";
        values.add(value);
        publishProgress(values);
    }

    private String getUTCdatetimeAsString() {
        final String DATEFORMAT = "yyyy-MM-dd'T'HH:mm:ss.SSS'Z'";// ISO8601 format, https://zh.wikipedia.org/wiki/ISO_8601
        final SimpleDateFormat sdf = new SimpleDateFormat(DATEFORMAT);
        sdf.setTimeZone(TimeZone.getTimeZone("UTC"));
        final String utcTime = sdf.format(new Date());

        return utcTime;
    }

    /* Simulate the real wind power */
    private double getPower(int light, double depreciation)
    {
        if (light <= 3)
            return 0;
        else if (light <= 7)
            return (light - 3) * 50 * depreciation;
        else if (light <= 9)
            return (light - 7) * 100 * depreciation + 200;
        else if (light < 12)
            return (light - 9) * 200 * depreciation + 400;
        else
            return 1000 * depreciation;
    }

    /**
     * Used as a counter in the message callback.
     */
    protected static class Counter {
        protected int num;

        public Counter(int num) {
            this.num = num;
        }

        public int get() {
            return this.num;
        }

        public void increment() {
            this.num++;
        }

        @Override
        public String toString() {
            return Integer.toString(this.num);
        }
    }

    public interface ProgressUpdateCallback {
        void callback(ArrayList<String> values);
    }
}
