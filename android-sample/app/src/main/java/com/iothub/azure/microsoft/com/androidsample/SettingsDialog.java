package com.iothub.azure.microsoft.com.androidsample;

import android.app.Activity;
import android.app.Dialog;
import android.content.DialogInterface;
import android.support.v7.app.AlertDialog;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.widget.EditText;

/**
 * Created by a-walin on 2017/5/22.
 */

public class SettingsDialog {
    public void showDialog(final MainActivity activity, DialogInterface.OnClickListener onClickListener) {
        AlertDialog.Builder builder = new AlertDialog.Builder(activity);
        // Get the layout inflater
        LayoutInflater inflater = activity.getLayoutInflater();

        // Inflate and set the layout for the dialog
        // Pass null as the parent view because its going in the dialog layout
        View content = inflater.inflate(R.layout.dialog_connectionstring, null);
        final EditText editText = (EditText)content.findViewById(R.id.edittext_connectionstring);
        editText.setText(activity.connString);
        builder.setView(content)
                // Add action buttons
                .setPositiveButton(R.string.confirm, onClickListener)
                .setNegativeButton(R.string.cancel, new DialogInterface.OnClickListener() {
                    public void onClick(DialogInterface dialog, int id) {
                        Log.d("SettingsDialog", "setNegativeButton");
                        //LoginDialogFragment.this.getDialog().cancel();
                    }
                });
        builder.create();
        builder.show();
    }

}
