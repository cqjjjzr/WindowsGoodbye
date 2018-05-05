package com.github.teamclc.windowsgoodbye;

import android.app.AlertDialog;
import android.content.DialogInterface;
import android.content.Intent;
import android.os.Bundle;
import android.support.v7.app.AppCompatActivity;
import android.support.v7.widget.LinearLayoutManager;
import android.support.v7.widget.RecyclerView;
import android.text.InputType;
import android.view.View;
import android.widget.EditText;
import android.widget.ImageButton;
import android.widget.TextView;
import android.widget.Toast;

import com.github.teamclc.windowsgoodbye.db.DbHelper;
import com.github.teamclc.windowsgoodbye.model.AuthRecord;
import com.github.teamclc.windowsgoodbye.model.PCInfo;
import com.github.teamclc.windowsgoodbye.ui.AuthRecordListAdapter;

import java.util.Date;
import java.util.List;

public class PCDetailsActivity extends AppCompatActivity implements View.OnClickListener {
    public static final String EXTRA_NAME = "pcinfo";
    public static final String RESULT_EXTRA_NAME = "newName";
    public static final String RESULT_EXTRA_ID_NAME = "deviceID";
    public static final int RESULT_CODE = 12450;

    private PCInfo info;
    private TextView displayNameView;
    private TextView deviceIDView;
    private RecyclerView authRecordsView;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_pcdetails);
        Intent starter = getIntent();
        info = (PCInfo) getIntent().getSerializableExtra(EXTRA_NAME);
        if (info == null) {
            finish();
            return;
        }

        displayNameView = findViewById(R.id.displayName);
        deviceIDView = findViewById(R.id.deviceIDDetails);
        authRecordsView = findViewById(R.id.authRecords);

        displayNameView.setText(info.getComputerInfo());
        deviceIDView.setText(info.getDeviceID().toString());

        List<AuthRecord> records = new DbHelper(this).getAuthRecordsByID(info.getDeviceID());
        for (int i = 0;i < 50;i++)
            records.add(new AuthRecord(info.getDeviceID(), new Date()));
        authRecordsView.setLayoutManager(new LinearLayoutManager(this));
        authRecordsView.setAdapter(new AuthRecordListAdapter(records));

        ImageButton editButton = findViewById(R.id.edit_display_name);
        editButton.setOnClickListener(this);
    }

    @Override
    public void onClick(View v) {
        AlertDialog.Builder builder = new AlertDialog.Builder(this);
        builder.setTitle(getString(R.string.change_display_name));

        final EditText input = new EditText(this);
        input.setText(info.getComputerInfo());
        input.setInputType(InputType.TYPE_CLASS_TEXT);
        input.setSelection(0, info.getComputerInfo().length());
        builder.setView(input);
        input.requestFocus();

        builder.setPositiveButton(android.R.string.ok, new DialogInterface.OnClickListener() {
            @Override
            public void onClick(DialogInterface dialog, int which) {
                String newName = input.getText().toString();
                if (newName.trim().isEmpty()) {
                    Toast.makeText(PCDetailsActivity.this, R.string.invalid_name, Toast.LENGTH_SHORT).show();
                }
                info.setComputerInfo(newName);
                new DbHelper(PCDetailsActivity.this).changeDisplayName(info.getDeviceID(), newName);
                displayNameView.setText(newName);
                Intent res = getIntent();
                res.putExtra(RESULT_EXTRA_NAME, newName);
                res.putExtra(RESULT_EXTRA_ID_NAME, info.getDeviceID());
                setResult(RESULT_CODE, res);
            }
        });
        builder.setNegativeButton(android.R.string.cancel, new DialogInterface.OnClickListener() {
            @Override
            public void onClick(DialogInterface dialog, int which) {
                dialog.cancel();
            }
        });

        builder.show();
    }
}
