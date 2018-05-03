package com.github.teamclc.windowsgoodbye.ui;

import android.app.Activity;
import android.app.AlertDialog;
import android.content.DialogInterface;
import android.content.Intent;
import android.support.annotation.NonNull;
import android.support.v7.widget.RecyclerView;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.CompoundButton;
import android.widget.ImageButton;
import android.widget.Switch;
import android.widget.TextView;

import com.github.teamclc.windowsgoodbye.PCDetailsActivity;
import com.github.teamclc.windowsgoodbye.R;
import com.github.teamclc.windowsgoodbye.db.DbHelper;
import com.github.teamclc.windowsgoodbye.model.PCInfo;

import java.util.List;
import java.util.UUID;

public class PCInfoListAdapter extends android.support.v7.widget.RecyclerView.Adapter<PCInfoListAdapter.PCInfoItemHolder> {
    private Activity ctx;
    private List<PCInfo> infos;
    public PCInfoListAdapter(Activity ctx) {
        this.ctx = ctx;
        infos = new DbHelper(ctx).getAllPCInfoNoKey();
        Log.d("PCInfoListAdapter", "Rows:" + infos.size());
    }

    @NonNull
    @Override
    public PCInfoListAdapter.PCInfoItemHolder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        View v = LayoutInflater.from(parent.getContext()).inflate(R.layout.pc_card, parent, false);
        return new PCInfoItemHolder(v);
    }

    @Override
    public void onBindViewHolder(@NonNull PCInfoListAdapter.PCInfoItemHolder holder, int position) {
        PCInfo info = infos.get(position);
        holder.pcNameView.setText(info.getComputerInfo());
        holder.deviceIdView.setText(info.getDeviceID().toString());
        if (info.getLastUsedTime() != null)
            holder.lastUsedTimeView.setText(ctx.getString(R.string.last_used_prompt, info.getLastUsedTime().toString()));
        else
            holder.lastUsedTimeView.setText(ctx.getString(R.string.last_used_prompt_never));
        holder.enabledSwitch.setChecked(info.isEnabled());
        Log.d("PCInfoListAdapter", "Binding:" + info.getDeviceID().toString());
    }

    @Override
    public int getItemCount() {
        return infos.size();
    }

    private void showPCInfoDetails(int adapterPosition) {
        Intent starter = new Intent(ctx, PCDetailsActivity.class);
        starter.putExtra(PCDetailsActivity.EXTRA_NAME, infos.get(adapterPosition));
        ctx.startActivityForResult(starter, 0);
    }

    private void saveEnabledChanged(int adapterPosition, boolean enabled) {
        PCInfo info = infos.get(adapterPosition);
        new DbHelper(ctx).changeEnabled(info.getDeviceID(), enabled);
    }

    public void notifyItemChanged(UUID deviceID, String newName) {
        int i = 0;
        for (PCInfo info : infos) {
            if (info.getDeviceID().equals(deviceID)) {
                info.setComputerInfo(newName);
                break;
            }
            i++;
        }
        if (i < infos.size()) notifyItemChanged(i);
    }

    class PCInfoItemHolder extends RecyclerView.ViewHolder {
        TextView pcNameView;
        TextView deviceIdView;
        TextView lastUsedTimeView;
        Switch enabledSwitch;

        PCInfoItemHolder(final View itemView) {
            super(itemView);

            pcNameView = itemView.findViewById(R.id.pcName);
            deviceIdView = itemView.findViewById(R.id.deviceID);
            lastUsedTimeView = itemView.findViewById(R.id.lastUsedTime);
            enabledSwitch = itemView.findViewById(R.id.enabledButton);

            itemView.findViewById(R.id.pcInfoContainer).setOnClickListener(new View.OnClickListener() {
                @Override
                public void onClick(View v) {
                    showPCInfoDetails(getAdapterPosition());
                }
            });
            enabledSwitch.setOnCheckedChangeListener(new CompoundButton.OnCheckedChangeListener() {
                @Override
                public void onCheckedChanged(CompoundButton buttonView, boolean isChecked) {
                    saveEnabledChanged(getAdapterPosition(), isChecked);
                }
            });

            ImageButton deleteButton = itemView.findViewById(R.id.deleteButton);
            deleteButton.setOnClickListener(new View.OnClickListener() {
                @Override
                public void onClick(final View v) {
                    final PCInfo info = infos.get(getAdapterPosition());
                    new AlertDialog.Builder(v.getContext())
                            .setTitle(android.R.string.dialog_alert_title)
                            .setMessage(v.getContext().getString(R.string.delete_confimation, info.getComputerInfo(), info.getDeviceID().toString()))
                            .setPositiveButton(android.R.string.ok, new DialogInterface.OnClickListener() {
                                @Override
                                public void onClick(DialogInterface dialog, int which) {
                                    new DbHelper(v.getContext()).deletePCInfo(info);
                                    infos.remove(getAdapterPosition());
                                    notifyItemRemoved(getAdapterPosition());
                                }
                            })
                            .setNegativeButton(android.R.string.cancel, new DialogInterface.OnClickListener() {
                                @Override
                                public void onClick(DialogInterface dialog, int which) {
                                    dialog.cancel();
                                }
                            })
                            .show();
                }
            });
        }
    }
}
