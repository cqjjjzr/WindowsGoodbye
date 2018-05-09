package com.github.teamclc.windowsgoodbye;

import android.annotation.SuppressLint;
import android.app.Notification;
import android.app.PendingIntent;
import android.app.Service;
import android.content.Context;
import android.content.Intent;
import android.os.AsyncTask;
import android.os.IBinder;
import android.support.v4.app.NotificationCompat;
import android.util.Log;

import java.io.IOException;
import java.net.DatagramPacket;
import java.net.DatagramSocket;
import java.net.InetAddress;
import java.net.InetSocketAddress;
import java.net.MulticastSocket;
import java.net.UnknownHostException;

public class UDPListeningService extends Service {
    public volatile static boolean state = false;
    public static final int SERVICE_ID = 5135;
    public static final int BUFFER_SIZE = 2048;
    public static final int MULTICAST_PORT = 26817;
    public static final int UNICAST_CAST = 26818;
    public static InetAddress MULTICAST_GROUP;
    public MulticastListeningTask mtask;
    public UnicastListeningTask utask;

    static {
        try {
            MULTICAST_GROUP = InetAddress.getByName("225.67.76.67");
        } catch (UnknownHostException e) { /* IGNORED */ } // THIS SHOULD NEVER HAPPENS
    }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        state = true;

        Log.d("multicast", "onStartCommand");
        Intent notificationIntent = new Intent(this, MainActivity.class);

        Log.i("multicast", "try to start multicast");
        mtask = new MulticastListeningTask();
        mtask.executeOnExecutor(AsyncTask.THREAD_POOL_EXECUTOR);
        utask = new UnicastListeningTask();
        utask.executeOnExecutor(AsyncTask.THREAD_POOL_EXECUTOR);
        PendingIntent pendingIntent = PendingIntent.getActivity(this, 0,
                notificationIntent, 0);
        Notification notification = new NotificationCompat.Builder(this, "default")
                .setSmallIcon(R.mipmap.ic_launcher)
                .setPriority(Notification.PRIORITY_LOW)
                .setContentTitle("Windows Goodbye")
                .setContentText("No pending auth request.")
                .setContentIntent(pendingIntent).build();

        startForeground(SERVICE_ID, notification);
        return START_STICKY;
    }

    @Override
    public IBinder onBind(Intent intent) {
        return null;
    }

    @Override
    public void onDestroy() {
        if (mtask != null && !mtask.isCancelled()) mtask.cancel(true);
        mtask = null;
        state = false;
        stopForeground(true);
    }

    public static void startup(Context context) {
        Intent starter = new Intent(context, UDPListeningService.class);
        //starter.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
        Log.i("multicast", "try to start multicast serv");
        context.startService(starter);
    }

    @SuppressLint("StaticFieldLeak")
    private abstract class UDPListeningTask
         extends AsyncTask<Void, Object, Void> {
        private DatagramSocket socket;
        protected abstract DatagramSocket allocSocket() throws IOException;
        protected abstract void processData(byte[] content, int len, InetAddress from);
        protected abstract String tag();

        @Override
        protected Void doInBackground(Void... voids) {
            try {
                socket = allocSocket();
                publishProgress("started!!!");
                byte[] buf = new byte[BUFFER_SIZE];
                DatagramPacket packet = new DatagramPacket(buf, 0, BUFFER_SIZE);
                while (true) {
                    try {
                        if (this.isCancelled()) break;
                        socket.receive(packet);
                    } catch (Exception ex) {
                        publishProgress("inner exception!!!");
                    }
                    publishProgress("received!!!");
                    processData(buf, packet.getLength(), ((InetSocketAddress) packet.getSocketAddress()).getAddress());
                }
            } catch (Exception ex) {
                publishProgress("Failed to start udp!!!", ex);
                stopSelf();
            }
            publishProgress("exiting!!!");
            return null;
        }

        @Override
        protected void onProgressUpdate(Object... values) {
            if (values.length > 1)
                Log.w(tag(), (String) values[0], (Throwable) values[1]);
            else
                Log.i(tag(), (String) values[0]);
        }
    }

    @SuppressLint("StaticFieldLeak")
    private class MulticastListeningTask extends UDPListeningTask {
        @Override
        protected DatagramSocket allocSocket() throws IOException {
            return allocNewMulticastSocket();
        }

        @Override
        protected void processData(byte[] content, int len, InetAddress from) {
            IncomingPacketProcessor.processMulticast(content, len, from, UDPListeningService.this);
        }

        @Override
        protected String tag() {
            return "udp_multicast";
        }
    }

    @SuppressLint("StaticFieldLeak")
    private class UnicastListeningTask extends UDPListeningTask {
        @Override
        protected DatagramSocket allocSocket() throws IOException {
            return allocNewUnicastSocket();
        }

        @Override
        protected void processData(byte[] content, int len, InetAddress from) {
            IncomingPacketProcessor.processUnicast(content, len, from, UDPListeningService.this);
        }

        @Override
        protected String tag() {
            return "udp_unicast";
        }
    }

    public static MulticastSocket allocNewMulticastSocket() throws IOException {
        MulticastSocket socket = new MulticastSocket(MULTICAST_PORT);
        socket.joinGroup(MULTICAST_GROUP);
        socket.setLoopbackMode(true);
        return socket;
    }

    public static DatagramSocket activeUnicastSocket;
    public static DatagramSocket allocNewUnicastSocket() throws IOException {
        if (activeUnicastSocket != null && activeUnicastSocket.isBound())
            activeUnicastSocket.close();
        activeUnicastSocket = new DatagramSocket(UNICAST_CAST);
        return activeUnicastSocket;
    }
}
