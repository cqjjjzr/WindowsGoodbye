package com.github.teamclc.windowsgoodbye;

import android.annotation.SuppressLint;
import android.content.Intent;
import android.os.AsyncTask;
import android.os.Bundle;
import android.os.Handler;
import android.os.Message;
import android.support.design.widget.FloatingActionButton;
import android.support.design.widget.NavigationView;
import android.support.design.widget.Snackbar;
import android.support.v4.view.GravityCompat;
import android.support.v4.widget.DrawerLayout;
import android.support.v7.app.ActionBarDrawerToggle;
import android.support.v7.app.AppCompatActivity;
import android.support.v7.widget.DefaultItemAnimator;
import android.support.v7.widget.LinearLayoutManager;
import android.support.v7.widget.RecyclerView;
import android.support.v7.widget.Toolbar;
import android.util.Log;
import android.view.Menu;
import android.view.MenuItem;
import android.view.View;
import android.widget.Toast;

import com.github.teamclc.windowsgoodbye.model.PCInfo;
import com.github.teamclc.windowsgoodbye.ui.PCInfoListAdapter;
import com.google.zxing.integration.android.IntentIntegrator;
import com.google.zxing.integration.android.IntentResult;
import com.mtramin.rxfingerprint.RxFingerprint;

import java.io.IOException;
import java.util.Objects;
import java.util.UUID;

public class MainActivity extends AppCompatActivity
        implements NavigationView.OnNavigationItemSelectedListener {
    public static final int ADDED_HANDLER_WHAT = 4707764; // bilibili av4707764 sounds sooooooooooooooooo good

    private PCInfoListAdapter listAdapter;
    public static Handler msgHandler = null;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        initHandler();
        setContentView(R.layout.activity_main);
        Toolbar toolbar = findViewById(R.id.toolbar);
        setSupportActionBar(toolbar);

        if (!RxFingerprint.isAvailable(this)) {
            Toast.makeText(this, R.string.fingerprint_not_available, Toast.LENGTH_LONG).show();
            finish();
        }

        FloatingActionButton fab = findViewById(R.id.fab);
        fab.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                IntentIntegrator intentIntegrator = new IntentIntegrator(MainActivity.this);
                intentIntegrator.setPrompt(getString(R.string.qr_prompt));
                intentIntegrator.initiateScan();
            }
        });

        DrawerLayout drawer = findViewById(R.id.drawer_layout);
        ActionBarDrawerToggle toggle = new ActionBarDrawerToggle(
                this, drawer, toolbar, R.string.navigation_drawer_open, R.string.navigation_drawer_close);
        drawer.addDrawerListener(toggle);
        toggle.syncState();

        NavigationView navigationView = findViewById(R.id.nav_view);
        navigationView.setNavigationItemSelectedListener(this);

        UDPListeningService.startup(this);

        checkUriStart();
        //new DbHelper(this).notifyAdded(new PCInfo(UUID.randomUUID(), "sdoijfoij", "", true));

        RecyclerView recyclerView = findViewById(R.id.pcInfoList);
        recyclerView.setLayoutManager(new LinearLayoutManager(this));
        recyclerView.setItemAnimator(new DefaultItemAnimator());
        listAdapter = new PCInfoListAdapter(this);
        recyclerView.setAdapter(listAdapter);
    }

    @SuppressLint("HandlerLeak")
    private void initHandler() {
        msgHandler = new Handler() {
            @Override
            public void handleMessage(Message msg) {
                if (msg.what == ADDED_HANDLER_WHAT) {
                    String name = msg.getData().getString(PairingTools.ADDED_PCINFO_NAME_KEY);
                    Snackbar.make(MainActivity.this.findViewById(R.id.coordinatorLayout), getString(R.string.pair_successful, name), Snackbar.LENGTH_LONG).show();
                    listAdapter.notifyAdded();
                }
            }
        };
    }

    private void checkUriStart() {
        Intent intent = getIntent();
        if (intent.getAction() != null && intent.getAction().equals("android.intent.action.VIEW")) {
            String pairData = intent.getDataString();
            pairData = Objects.requireNonNull(pairData).replace("data:", "");
            Log.d("URLStart","Started with data:"+ pairData);
            new PairTask().execute(pairData);
        }
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        if (resultCode == PCDetailsActivity.RESULT_CODE) {
            UUID deviceID = (UUID) data.getSerializableExtra(PCDetailsActivity.RESULT_EXTRA_ID_NAME);
            if (deviceID == null) return;
            String newName = data.getStringExtra(PCDetailsActivity.RESULT_EXTRA_NAME);
            listAdapter.notifyItemChanged(deviceID, newName);
            return;
        }
        IntentResult result = IntentIntegrator.parseActivityResult(requestCode, resultCode, data);
        if(result != null) {
            if (result.getContents() != null) {
                String sresult = result.getContents();
                Log.d("QRCode", "Have scan result in your app activity :" + sresult);
                new PairTask().execute(sresult);
            }
        } else {
            super.onActivityResult(requestCode, resultCode, data);
        }
    }

    @SuppressLint("StaticFieldLeak")
    private class PairTask extends AsyncTask<String, Void, Void> {
        @Override
        protected Void doInBackground(String... strings) {
            try {
                Log.d("PairTask", "start");
                PairingTools.processPairData(strings[0], MainActivity.this);
            } catch (IOException e) {
                Toast.makeText(MainActivity.this, getString(R.string.toast_pair_failed, e.getClass().getSimpleName(), e.getLocalizedMessage()), Toast.LENGTH_LONG).show();
            }
            return null;
        }
    }

    @Override
    public void onBackPressed() {
        DrawerLayout drawer = findViewById(R.id.drawer_layout);
        if (drawer.isDrawerOpen(GravityCompat.START)) {
            drawer.closeDrawer(GravityCompat.START);
        } else {
            super.onBackPressed();
        }
    }

    @Override
    public boolean onCreateOptionsMenu(Menu menu) {
        // Inflate the menu; this adds items to the action bar if it is present.
        getMenuInflater().inflate(R.menu.main, menu);
        return true;
    }

    @Override
    public boolean onOptionsItemSelected(MenuItem item) {
        // Handle action bar item clicks here. The action bar will
        // automatically handle clicks on the Home/Up button, so long
        // as you specify a parent activity in AndroidManifest.xml.
        int id = item.getItemId();

        //noinspection SimplifiableIfStatement
        if (id == R.id.action_settings) {
            return true;
        }

        return super.onOptionsItemSelected(item);
    }

    @SuppressWarnings("StatementWithEmptyBody")
    @Override
    public boolean onNavigationItemSelected(MenuItem item) {
        // Handle navigation view item clicks here.
        int id = item.getItemId();

        if (id == R.id.nav_camera) {
            // Handle the camera action
        } else if (id == R.id.nav_gallery) {

        } else if (id == R.id.nav_slideshow) {

        } else if (id == R.id.nav_manage) {

        } else if (id == R.id.nav_share) {

        } else if (id == R.id.nav_send) {

        }

        DrawerLayout drawer = (DrawerLayout) findViewById(R.id.drawer_layout);
        drawer.closeDrawer(GravityCompat.START);
        return true;
    }

    @Override
    protected void onDestroy() {
        super.onDestroy();
        msgHandler = null;
    }
}
