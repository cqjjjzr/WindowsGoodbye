package com.github.teamclc.windowsgoodbye.db;

import android.annotation.SuppressLint;
import android.content.ContentValues;
import android.content.Context;
import android.database.Cursor;
import android.database.sqlite.SQLiteDatabase;
import android.database.sqlite.SQLiteOpenHelper;
import android.util.Log;

import com.github.teamclc.windowsgoodbye.model.AuthRecord;
import com.github.teamclc.windowsgoodbye.model.PCInfo;

import java.sql.Timestamp;
import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.Date;
import java.util.List;
import java.util.UUID;

public class DbHelper extends SQLiteOpenHelper {
    private static final int DB_VERSION = 1;
    private static final String DB_NAME = "myTest.db";
    private static final String TABLE_NAME_PCINFO = "pcInfos";
    private static final String TABLE_NAME_AUTH_RECORD = "records";
    private static final String COLUMN_ID = "Id";
    private static final String COLUMN_DEVICE_ID = "deviceID";
    private static final String COLUMN_DISPLAY_NAME = "displayName";
    private static final String COLUMN_KEYS = "keys";
    private static final String COLUMN_ENABLED = "enabled";
    private static final String COLUMN_TIME = "time";

    public DbHelper(Context context) {
        super(context, DB_NAME, null, DB_VERSION);
    }

    @Override
    public void onCreate(SQLiteDatabase db) {
        Log.d("DbHelper", "OnCreate!");
        String sql = "create table if not exists " + TABLE_NAME_PCINFO + " ("
                + COLUMN_ID + " integer primary key autoincrement, "
                + COLUMN_DEVICE_ID + " varchar(36), "
                + COLUMN_DISPLAY_NAME + " text, "
                + COLUMN_KEYS + " text, "
                + COLUMN_ENABLED +  " integer"
                + ")";
        Log.d("DBHelper", "executing sql: " + sql);
        db.execSQL(sql);

        sql = "create table if not exists " + TABLE_NAME_AUTH_RECORD + " ("
                + COLUMN_ID + " integer primary key autoincrement, "
                + COLUMN_DEVICE_ID + " varchar(36), "
                + COLUMN_TIME + " TIMESTAMP default CURRENT_TIMESTAMP"
                + ")";
        Log.d("DBHelper", "executing sql: " + sql);
        db.execSQL(sql);
    }

    @Override
    public void onUpgrade(SQLiteDatabase db, int oldVersion, int newVersion) {
        if (oldVersion == newVersion) return;
        Log.d("DbHelper", "OnUpgrade!");
        String sql = "drop table if exists " + TABLE_NAME_PCINFO;
        Log.d("DBHelper", "executing sql: " + sql);
        db.execSQL(sql);
        sql = "drop table if exists " + TABLE_NAME_AUTH_RECORD;
        Log.d("DBHelper", "executing sql: " + sql);
        db.execSQL(sql);
        onCreate(db);
    }

    public void addPCInfo(PCInfo info) {
        SQLiteDatabase db = getWritableDatabase();
        db.delete(TABLE_NAME_PCINFO, COLUMN_DISPLAY_NAME + " IS NULL", null);
        ContentValues values = new ContentValues();
        values.put(COLUMN_DEVICE_ID, info.getDeviceID().toString());
        values.put(COLUMN_DISPLAY_NAME, info.getComputerInfo());
        values.put(COLUMN_KEYS, info.getEncryptedKeys());
        values.put(COLUMN_ENABLED, info.isEnabled() ? 1 : 0);
        db.insertOrThrow(TABLE_NAME_PCINFO, null, values);
        db.close();
    }

    public void addAuthRecord(AuthRecord record) {
        SQLiteDatabase db = getWritableDatabase();
        ContentValues values = new ContentValues();
        values.put(COLUMN_DEVICE_ID, record.getDeviceID().toString());
        values.put(COLUMN_TIME, toSQLiteTimestamp(record.getTime()));
        db.insertOrThrow(TABLE_NAME_AUTH_RECORD, null, values);
        db.close();
    }

    public void changeDisplayName(UUID deviceID, String newDisplayName) {
        SQLiteDatabase db = getWritableDatabase();
        ContentValues values = new ContentValues();
        values.put(COLUMN_DISPLAY_NAME, newDisplayName);
        db.update(TABLE_NAME_PCINFO, values, COLUMN_DEVICE_ID + " = ?", new String[]{ deviceID.toString() });
        db.close();
    }

    public void finishActivePairing(String displayName) {
        SQLiteDatabase db = getWritableDatabase();
        ContentValues values = new ContentValues();
        values.put(COLUMN_DISPLAY_NAME, displayName);
        db.update(TABLE_NAME_PCINFO, values, COLUMN_DISPLAY_NAME + " IS NULL", null);
        db.close();
    }

    public void changeEnabled(UUID deviceID, boolean enabled) {
        SQLiteDatabase db = getWritableDatabase();
        ContentValues values = new ContentValues();
        values.put(COLUMN_ENABLED, enabled ? 1 : 0);
        db.update(TABLE_NAME_PCINFO, values, COLUMN_DEVICE_ID + " = ?", new String[]{ deviceID.toString() });
        db.close();
    }

    public void deletePCInfo(PCInfo info) {
        SQLiteDatabase db = getWritableDatabase();
        String[] whereArgs =  new String[]{ info.getDeviceID().toString() };
        db.delete(TABLE_NAME_PCINFO, COLUMN_DEVICE_ID + " = ?", whereArgs);
        db.delete(TABLE_NAME_AUTH_RECORD, COLUMN_DEVICE_ID + " = ?", whereArgs);
        db.close();
    }

    private static final String[] COLUMNS_PCINFO_NOKEY = {
            COLUMN_DEVICE_ID, COLUMN_DISPLAY_NAME, COLUMN_ENABLED
    };
    private static final String[] COLUMNS_AUTHRECORD = {
            COLUMN_TIME
    };
    public List<PCInfo> getAllPCInfoNoKey() {
        SQLiteDatabase db = getReadableDatabase();
        Cursor cur = db.query(TABLE_NAME_PCINFO, COLUMNS_PCINFO_NOKEY, COLUMN_DISPLAY_NAME + " IS NOT NULL", null, null, null, null);
        ArrayList<PCInfo> result = new ArrayList<>(cur.getColumnCount());
        while (cur.moveToNext()) {
            String idString = cur.getString(0);
            PCInfo info = new PCInfo(UUID.fromString(idString), cur.getString(1));
            info.setEnabled(cur.getInt(2) != 0);
            Cursor cur2 = db.query(
                    TABLE_NAME_AUTH_RECORD,
                    COLUMNS_AUTHRECORD,
                    COLUMN_DEVICE_ID + " = ?", new String[]{idString},
                    null,
                    null,
                    COLUMN_TIME + " DESC");

            if (cur2.moveToNext())
                info.setLastUsedTime(Timestamp.valueOf(cur2.getString(0)));
            cur2.close();
            result.add(info);
        }
        cur.close();
        db.close();
        return result;
    }

    private static final String[] COLUMNS_PCINFO_WITHKEY = {
            COLUMN_DISPLAY_NAME, COLUMN_KEYS, COLUMN_ENABLED
    };
    public PCInfo getPCInfoByID(UUID deviceID) {
        SQLiteDatabase db = getReadableDatabase();
        Cursor cur = db.query(
                TABLE_NAME_PCINFO,
                COLUMNS_PCINFO_WITHKEY,
                COLUMN_DEVICE_ID + " = ?",
                new String[]{deviceID.toString()},
                null, null, null);
        PCInfo info = null;
        if (cur.moveToNext())
            info = new PCInfo(
                    deviceID, // id
                    cur.getString(0),  // name
                    cur.getString(1),
                    cur.getInt(2) != 0);
        cur.close();
        db.close();
        return info;
    }

    public List<AuthRecord> getAuthRecordsByID(UUID deviceID) {
        SQLiteDatabase db = getReadableDatabase();
        Cursor cur = db.query(
                TABLE_NAME_AUTH_RECORD,
                COLUMNS_AUTHRECORD,
                COLUMN_DEVICE_ID + " = ?", new String[]{deviceID.toString()},
                null,
                null,
                COLUMN_TIME + " DESC");
        ArrayList<AuthRecord> result = new ArrayList<>(cur.getColumnCount());
        while (cur.moveToNext())
            result.add(new AuthRecord(deviceID, Timestamp.valueOf(cur.getString(0))));
        cur.close();
        db.close();
        return result;
    }

    @SuppressLint("SimpleDateFormat")
    private String toSQLiteTimestamp(Date date) {
        return new SimpleDateFormat("yyyy-MM-dd HH:mm:ss").format(date);
    }
}
