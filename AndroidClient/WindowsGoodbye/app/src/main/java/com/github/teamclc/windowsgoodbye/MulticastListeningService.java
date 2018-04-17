package com.github.teamclc.windowsgoodbye;

import android.app.Service;
import android.content.Context;
import android.content.Intent;
import android.os.IBinder;

public class MulticastListeningService extends Service {
    public static final String ACTION = "com.github.teamclc.windowsgoodbye.MulticastListeningService";
    public volatile static boolean state = false;
    public MulticastListeningService() {
    }

    private volatile Thread workingThread;

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        state = true;
        return START_STICKY;
    }

    @Override
    public IBinder onBind(Intent intent) {
        // TODO: Return the communication channel to the service.
        return null;
    }

    @Override
    public void onDestroy() {
        workingThread.interrupt();
        workingThread = null;
        state = false;
    }

    public static void startup(Context context) {
        Intent starter = new Intent(ACTION);
        starter.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
        context.startService(starter);
    }
}
