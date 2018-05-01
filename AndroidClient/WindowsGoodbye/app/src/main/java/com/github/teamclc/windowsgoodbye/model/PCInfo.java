package com.github.teamclc.windowsgoodbye.model;

import java.util.UUID;

public class PCInfo {
    public static final int KEYS_LENGTH = 32;

    private UUID deviceID;
    private byte[] deviceKey;
    private byte[] authKey;
    private String computerInfo;

    public PCInfo(UUID deviceID, byte[] deviceKey, byte[] authKey) {
        this.deviceID = deviceID;
        this.deviceKey = deviceKey;
        this.authKey = authKey;
    }

    public UUID getDeviceID() {
        return deviceID;
    }

    public void setDeviceID(UUID deviceID) {
        this.deviceID = deviceID;
    }

    public byte[] getDeviceKey() {
        return deviceKey;
    }

    public void setDeviceKey(byte[] deviceKey) {
        this.deviceKey = deviceKey;
    }

    public byte[] getAuthKey() {
        return authKey;
    }

    public void setAuthKey(byte[] authKey) {
        this.authKey = authKey;
    }

    public String getComputerInfo() {
        return computerInfo;
    }

    public void setComputerInfo(String computerInfo) {
        this.computerInfo = computerInfo;
    }
}
