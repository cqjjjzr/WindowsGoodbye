package com.github.teamclc.windowsgoodbye;

import android.app.Notification;
import android.app.PendingIntent;
import android.app.Service;
import android.content.Context;
import android.content.Intent;
import android.os.IBinder;
import android.support.v4.app.NotificationCompat;

import java.net.DatagramPacket;
import java.net.InetAddress;
import java.net.MulticastSocket;
import java.net.UnknownHostException;

public class MulticastListeningService extends Service {
    public static final String ACTION = "com.github.teamclc.windowsgoodbye.MulticastListeningService";
    public volatile static boolean state = false;
    public static final int SERVICE_ID = 5135;
    public static final int BUFFER_SIZE = 2048;
    public static final int PORT = 26817;
    public static InetAddress MULTICAST_GROUP;
    public static MulticastSocket multicastSocket;

    public MulticastListeningService() {
        try {
            MULTICAST_GROUP = InetAddress.getByName("225.67.76.67");
        } catch (UnknownHostException e) { /* IGNORED */ } // THIS SHOULD NEVER HAPPENS
    }

    private volatile Thread workingThread;

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        state = true;

        Intent notificationIntent = new Intent(this, MainActivity.class);

        PendingIntent pendingIntent = PendingIntent.getActivity(this, 0,
                notificationIntent, 0);
        Notification notification = new NotificationCompat.Builder(this, "default")
                .setSmallIcon(R.mipmap.ic_launcher)
                .setPriority(Notification.PRIORITY_LOW)
                .setContentTitle("Windows Goodbye")
                .setContentText("No pending auth request.")
                .setContentIntent(pendingIntent).build();

        startForeground(SERVICE_ID, notification);

        workingThread = new Thread(new MulticastListeningRunnable());
        workingThread.start();
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
        stopForeground(true);
    }

    public static void startup(Context context) {
        Intent starter = new Intent(context, MulticastListeningService.class);
        starter.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
        context.startService(starter);
    }

    private class MulticastListeningRunnable implements Runnable {
        @Override
        public void run() {
            try {
                multicastSocket = new MulticastSocket(PORT);
                multicastSocket.joinGroup(MULTICAST_GROUP);
                multicastSocket.setLoopbackMode(true);
                byte[] buf = new byte[BUFFER_SIZE];
                DatagramPacket packet = new DatagramPacket(buf, 0, BUFFER_SIZE);
                while (true) {
                    if (workingThread.isInterrupted()) break;
                    multicastSocket.receive(packet);
                    // TODO resolve packet!
                }
            } catch (Exception ex) {
                stopSelf();
            }
        }
    }
}
