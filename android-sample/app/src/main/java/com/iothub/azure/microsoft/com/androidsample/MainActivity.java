package com.iothub.azure.microsoft.com.androidsample;

import android.content.Context;
import android.content.DialogInterface;
import android.content.SharedPreferences;
import android.os.AsyncTask;
import android.support.v7.app.AlertDialog;
import android.support.v7.app.AppCompatActivity;
import android.os.Bundle;
import android.support.v7.widget.Toolbar;
import android.util.Log;
import android.view.Menu;
import android.view.MenuItem;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.SeekBar;
import android.widget.TextView;
import android.widget.Toast;

import com.microsoft.azure.sdk.iot.device.IotHubClientProtocol;

import java.util.ArrayList;

import static android.content.DialogInterface.BUTTON_NEUTRAL;
import static android.content.DialogInterface.BUTTON_POSITIVE;

public class MainActivity extends AppCompatActivity {

    private static final String TAG = "MainActivity";

    private static final String defaultConnectionString = "<Please put your device connection string here>";
    public static String connString;

    //public static IotHubClientProtocol protocol = IotHubClientProtocol.HTTPS;
    public static IotHubClientProtocol protocol = IotHubClientProtocol.MQTT;

    private MainActivity mainActivity;
    Context context;
    Button btnSendMessage;
    TextView sendMessage;
    TextView receiveMessage;
    boolean isStartingToSend = false;
    private SendMessageAsyncTask sendTask;
    TextView seekbarSpeedText;
    TextView seekbarDepreciationText;
    SeekBar seekbarSpeed;
    SeekBar seekbarDepreciation;
    Toolbar toolbarMain;
    int speed;
    double depreciation;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);
        this.context = this;
        this.mainActivity = this;
        this.connString = getPreferencesConnectionString();
        findAllViews();
    }

    @Override
    public boolean onCreateOptionsMenu(Menu menu) {
        // Inflate the menu; this adds items to the action bar if it is present.
        getMenuInflater().inflate(R.menu.menu_main, menu);
        return true;
    }

    private void findAllViews() {
        toolbarMain = (Toolbar) findViewById(R.id.toolbar);
        toolbarMain.setTitle(getString(R.string.app_name));
        setSupportActionBar(toolbarMain);
        toolbarMain.setOnMenuItemClickListener(onMenuItemClick);

        btnSendMessage = (Button) findViewById(R.id.btnSend);
        sendMessage = (TextView) findViewById(R.id.textSend);
        receiveMessage = (TextView) findViewById(R.id.textReceive);
        seekbarSpeedText = (TextView) findViewById(R.id.seekbarLightText);
        seekbarDepreciationText = (TextView) findViewById(R.id.seekbarDepreciationText);
        seekbarSpeed = (SeekBar) findViewById(R.id.seekbarLight);
        seekbarSpeed.setOnSeekBarChangeListener(new SeekBar.OnSeekBarChangeListener() {
            @Override
            public void onProgressChanged(SeekBar seekBar, int progress, boolean fromUser) {
                saveAndUpdateSpeed(progress);
            }

            @Override
            public void onStartTrackingTouch(SeekBar seekBar) {

            }

            @Override
            public void onStopTrackingTouch(SeekBar seekBar) {
                saveAndUpdateSpeed(seekBar.getProgress());
            }
        });

        saveAndUpdateSpeed(seekbarSpeed.getProgress());

        seekbarDepreciation = (SeekBar) findViewById(R.id.seekbarDepreciation);
        seekbarDepreciation.setOnSeekBarChangeListener(new SeekBar.OnSeekBarChangeListener() {
            @Override
            public void onProgressChanged(SeekBar seekBar, int progress, boolean fromUser) {

                double depreciation = (double)progress / 100;
                saveAndUpdateDepreciation(depreciation);
            }

            @Override
            public void onStartTrackingTouch(SeekBar seekBar) {

            }

            @Override
            public void onStopTrackingTouch(SeekBar seekBar) {
                double depreciation = (double)seekBar.getProgress() / 100;
                saveAndUpdateDepreciation(depreciation);
            }
        });

        getAndUpdateDepreciation();
    }

    private DialogInterface.OnClickListener onClickListener = new DialogInterface.OnClickListener() {
        @Override
        public void onClick(DialogInterface dialogInterface, int i) {

            if(i == BUTTON_POSITIVE) {
                Log.d("SettingsDialog", "setPositiveButton");

                EditText edit = (EditText) ((AlertDialog) dialogInterface).findViewById(R.id.edittext_connectionstring);
                connString = edit.getText().toString();
                updatePreferencesConnectionString(connString);
            }
        }
    };

    private Toolbar.OnMenuItemClickListener onMenuItemClick = new Toolbar.OnMenuItemClickListener() {
        @Override
        public boolean onMenuItemClick(MenuItem item) {
            switch (item.getItemId()) {
                case R.id.action_settings:
                    Log.d(TAG, "action_settings");
                    SettingsDialog dialog = new SettingsDialog();
                    dialog.showDialog(mainActivity, onClickListener);
                    break;
            }

            return true;
        }
    };

    private String getPreferencesConnectionString() {
        SharedPreferences sharedPref = this.getPreferences(Context.MODE_PRIVATE);
        return sharedPref.getString("ConnectionString", defaultConnectionString);
    }

    private void updatePreferencesConnectionString(String cs) {
        SharedPreferences sharedPref = this.getPreferences(Context.MODE_PRIVATE);
        SharedPreferences.Editor editor = sharedPref.edit();
        editor.putString("ConnectionString", cs);
        editor.commit();
    }

    private void saveAndUpdateSpeed(int light) {
        seekbarSpeedText.setText(getResources().getString(R.string.light)+": "+light);
        setSpeed(light);
    }

    private void saveAndUpdateDepreciation(double depreciation) {
        seekbarDepreciationText.setText(getResources().getString(R.string.depreciation)+": "+ depreciation);
        setDepreciation(depreciation);
    }

    private void getAndUpdateDepreciation() {
        double depreciation = (double)seekbarDepreciation.getProgress() / 100;
        saveAndUpdateDepreciation(depreciation);
    }

    @Override
    protected void onDestroy() {
        stopSendMessage();

        super.onDestroy();
    }

    public void btnSendOnClick(View v) {
        if (!isStartingToSend)
            sendMessage();
        else
            stopSendMessage();
    }

    private void sendMessage() {
        isStartingToSend = true;

        sendTask = new SendMessageAsyncTask(this, new SendMessageAsyncTask.ProgressUpdateCallback() {
            @Override
            public void callback(ArrayList<String> values) {
                String key = values.get(0);
                String value = values.get(1);
                if (key.equals("SEND"))
                    sendMessage.setText(value);
                else if (key.equals("RECEIVE")) {
                    receiveMessage.setText(value);

                    Toast.makeText(getContext(),
                            value,
                            Toast.LENGTH_SHORT).show();
                }
            }
        });
        sendTask.execute();

        btnSendMessage.setText("Stop");
    }

    private Context getContext() {
        return this.context;
    }

    private void stopSendMessage() {
        isStartingToSend = false;

        if (sendTask != null && sendTask.getStatus() != AsyncTask.Status.FINISHED)
            sendTask.cancel(true);

        sendMessage.setText(sendMessage.getText() + "(stopped)");
        btnSendMessage.setText("Run");
    }

    public double getDepreciation() {
        return depreciation;
    }

    public void setDepreciation(double depreciation) {
        this.depreciation = depreciation;
    }

    public int getSpeed() {
        return speed;
    }

    public void setSpeed(int speed) {
        this.speed = speed;
    }
}
