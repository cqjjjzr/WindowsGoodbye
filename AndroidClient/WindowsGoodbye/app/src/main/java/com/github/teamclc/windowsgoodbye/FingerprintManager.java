package com.github.teamclc.windowsgoodbye;

import android.app.AlertDialog;
import android.content.Context;
import android.content.DialogInterface;
import android.os.AsyncTask;
import android.util.Base64;
import android.util.Log;
import android.widget.Toast;

import com.github.teamclc.windowsgoodbye.model.PCInfo;
import com.mtramin.rxfingerprint.EncryptionMethod;
import com.mtramin.rxfingerprint.RxFingerprint;
import com.mtramin.rxfingerprint.data.FingerprintEncryptionResult;

import io.reactivex.disposables.Disposable;
import io.reactivex.functions.Consumer;

public class FingerprintManager {
    private static AlertDialog createAskForEncryptFingerprintDialog(Context context) {
        AlertDialog.Builder builder;
        builder = new AlertDialog.Builder(context, android.R.style.Theme_Material_Dialog_Alert);
        return builder.setTitle(context.getString(R.string.encryption))
                .setMessage(R.string.use_fingerprint_to_pair)
                .setNegativeButton(android.R.string.cancel, new DialogInterface.OnClickListener() {
                    public void onClick(DialogInterface dialog, int which) {
                        // give up
                    }
                })
                .setIcon(R.drawable.ic_fingerprint)
                .create();
    }

    public static void encryptByFingerprint(final Context context, final PCInfo info, final Consumer<String> onNext) {
        final AlertDialog dialog = createAskForEncryptFingerprintDialog(context);

        String str =
                Base64.encodeToString(info.getDeviceKey(), Base64.DEFAULT)
                        + " "
                        + Base64.encodeToString(info.getAuthKey(), Base64.DEFAULT);
        final Disposable disposable = RxFingerprint.encrypt(EncryptionMethod.AES, context, info.getDeviceID().toString(), str)
                .subscribe(new Consumer<FingerprintEncryptionResult>() {
                    @Override
                    public void accept(FingerprintEncryptionResult fingerprintEncryptionResult) throws Exception {
                        switch (fingerprintEncryptionResult.getResult()) {
                            case FAILED:
                                dialog.setMessage(context.getString(R.string.finger_not_recognized));
                                dialog.setIcon(R.drawable.ic_fingerprint_failed);
                                break;
                            case HELP:
                                dialog.setMessage(fingerprintEncryptionResult.getMessage());
                                dialog.setIcon(R.drawable.ic_fingerprint_failed);
                                break;
                            case AUTHENTICATED:
                                Toast.makeText(context, R.string.connecting, Toast.LENGTH_SHORT).show();
                                new OnNextTask().execute(onNext, fingerprintEncryptionResult.getEncrypted());
                                onNext.accept(fingerprintEncryptionResult.getEncrypted());
                                dialog.dismiss();
                                break;
                        }
                    }
                }, new Consumer<Throwable>() {
                    @Override
                    public void accept(Throwable throwable) throws Exception {

                    }
                });

        dialog.setCancelable(false);
        dialog.setButton(DialogInterface.BUTTON_NEGATIVE, context.getText(android.R.string.cancel), new DialogInterface.OnClickListener() {
            @Override
            public void onClick(DialogInterface dialog, int which) {
                disposable.dispose();
                dialog.dismiss();
            }
        });
        dialog.show();
    }

    @SuppressWarnings("unchecked")
    public static class OnNextTask extends AsyncTask<Object, Void, Void> {
        @Override
        protected Void doInBackground(Object... objects) {
            try {
                ((Consumer<String>) objects[0]).accept((String) objects[1]);
            } catch (Exception e) {
                Log.e("OnNextTask", "Error calling onNext func!", e);
                //e.printStackTrace();
            }
            return null;
        }
    }
}
