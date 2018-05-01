package com.github.teamclc.windowsgoodbye.ui;

import android.content.Context;
import android.support.annotation.NonNull;
import android.support.v7.widget.RecyclerView;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.Switch;
import android.widget.TextView;

import com.github.teamclc.windowsgoodbye.R;

public class PCInfoListAdapter extends android.support.v7.widget.RecyclerView.Adapter<RecyclerView.ViewHolder> {
    private Context ctx;
    public PCInfoListAdapter(Context ctx) {
        this.ctx = ctx;
    }

    @NonNull
    @Override
    public RecyclerView.ViewHolder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        View v = LayoutInflater.from(parent.getContext()).inflate(R.layout.pc_card, parent);
        return new PCInfoItemHolder(v);
    }

    @Override
    public void onBindViewHolder(@NonNull RecyclerView.ViewHolder holder, int position) {

    }

    @Override
    public int getItemCount() {
        return 0;
    }

    private void showPCInfoDetails(int adapterPosition) {

    }

    public class PCInfoItemHolder extends RecyclerView.ViewHolder {
        public TextView pcNameView;
        public TextView deviceIdView;
        public TextView lastUsedTimeView;

        public Switch enabledSwitch;
        public PCInfoItemHolder(final View itemView) {
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
        }
    }
}
