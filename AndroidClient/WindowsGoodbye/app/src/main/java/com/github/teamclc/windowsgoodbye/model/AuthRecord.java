package com.github.teamclc.windowsgoodbye.model;

import java.util.Date;
import java.util.UUID;

public class AuthRecord {
    private UUID deviceID;
    private Date time;

    public UUID getDeviceID() {
        return deviceID;
    }

    public void setDeviceID(UUID deviceID) {
        this.deviceID = deviceID;
    }

    public Date getTime() {
        return time;
    }

    public void setTime(Date time) {
        this.time = time;
    }
}
