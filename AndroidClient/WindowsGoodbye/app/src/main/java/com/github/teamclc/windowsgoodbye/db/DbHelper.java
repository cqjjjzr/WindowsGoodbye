package com.github.teamclc.windowsgoodbye.db;

import android.annotation.SuppressLint;
import android.content.ContentValues;
import android.content.Context;
import android.database.sqlite.SQLiteDatabase;
import android.database.sqlite.SQLiteOpenHelper;

import com.github.teamclc.windowsgoodbye.model.AuthRecord;
import com.github.teamclc.windowsgoodbye.model.PCInfo;

import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.UUID;

public class DbHelper extends SQLiteOpenHelper {
    private static final int DB_VERSION = 1;
    private static final String DB_NAME = "myTest.db";
    public static final String TABLE_NAME_PCINFO = "pcInfos";
    public static final String TABLE_NAME_AUTH_RECORD = "records";
    public static final String COLUMN_ID = "Id";
    public static final String COLUMN_DEVICE_ID = "deviceID";
    public static final String COLUMN_DISPLAY_NAME = "displayName";
    public static final String COLUMN_DEVICE_KEY = "deviceKey";
    public static final String COLUMN_AUTH_KEY = "authKey";
    public static final String COLUMN_TIME = "time";

    public DbHelper(Context context) {
        super(context, DB_NAME, null, DB_VERSION);
    }

    @Override
    public void onCreate(SQLiteDatabase db) {
        String sql = "create table if not exists " + TABLE_NAME_PCINFO + " ("
                + COLUMN_ID + " integer primary key autoincrement, "
                + COLUMN_DEVICE_ID + " varchar(36), "
                + COLUMN_DISPLAY_NAME + " text, "
                + COLUMN_DEVICE_KEY + " blob, "
                + COLUMN_AUTH_KEY + " blob"
                + ")";
        db.execSQL(sql);

        sql = "create table if not exists " + TABLE_NAME_AUTH_RECORD + " ("
                + COLUMN_ID + " integer primary key autoincrement, "
                + COLUMN_DEVICE_ID + " varchar(36), "
                + COLUMN_TIME + " TIMESTAMP default CURRENT_TIMESTAMP"
                + ")";
        db.execSQL(sql);
    }

    @Override
    public void onUpgrade(SQLiteDatabase db, int oldVersion, int newVersion) {
        String sql = "drop table if exists " + TABLE_NAME_PCINFO;
        db.execSQL(sql);
        sql = "drop table if exists " + TABLE_NAME_AUTH_RECORD;
        db.execSQL(sql);
        onCreate(db);
    }

    public void addPCInfo(PCInfo info) {
        SQLiteDatabase db = getWritableDatabase();
        ContentValues values = new ContentValues();
        values.put(COLUMN_DEVICE_ID, info.getDeviceID().toString());
        values.put(COLUMN_DISPLAY_NAME, info.getComputerInfo());
        values.put(COLUMN_DEVICE_KEY, info.getDeviceKey());
        values.put(COLUMN_AUTH_KEY, info.getAuthKey());
        db.insertOrThrow(TABLE_NAME_PCINFO, null, values);
    }

    public void addAuthRecord(AuthRecord record) {
        SQLiteDatabase db = getWritableDatabase();
        ContentValues values = new ContentValues();
        values.put(COLUMN_DEVICE_ID, record.getDeviceID().toString());
        values.put(COLUMN_TIME, toSQLiteTimestamp(record.getTime()));
        db.insertOrThrow(TABLE_NAME_AUTH_RECORD, null, values);
    }

    public void changeDisplayName(UUID deviceID, String newDisplayName) {
        SQLiteDatabase db = getWritableDatabase();
        ContentValues values = new ContentValues();
        values.put(COLUMN_DISPLAY_NAME, newDisplayName);
        db.update(TABLE_NAME_PCINFO, values, COLUMN_DEVICE_ID + " = ?", new String[]{ deviceID.toString() });
    }

    public void deletePCInfo(PCInfo info) {
        SQLiteDatabase db = getWritableDatabase();
        db.beginTransaction();
        String[] whereArgs =  new String[]{ info.getDeviceID().toString() };
        db.delete(TABLE_NAME_PCINFO, COLUMN_DEVICE_ID + " = ?", whereArgs);
        db.delete(TABLE_NAME_AUTH_RECORD, COLUMN_DEVICE_ID + " = ?", whereArgs);
        db.setTransactionSuccessful();
    }

    @SuppressLint("SimpleDateFormat")
    private String toSQLiteTimestamp(Date date) {
        return new SimpleDateFormat("yyyy-MM-dd HH:mm:ss").format(date);
    }
}
