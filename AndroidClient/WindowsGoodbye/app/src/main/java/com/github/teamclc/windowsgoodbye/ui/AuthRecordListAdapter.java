package com.github.teamclc.windowsgoodbye.ui;

import android.support.annotation.NonNull;
import android.support.v7.widget.RecyclerView;
import android.view.LayoutInflater;
import android.view.ViewGroup;
import android.widget.TextView;

import com.github.teamclc.windowsgoodbye.R;
import com.github.teamclc.windowsgoodbye.model.AuthRecord;

import java.text.SimpleDateFormat;
import java.util.List;

public class AuthRecordListAdapter extends RecyclerView.Adapter<AuthRecordListAdapter.AuthRecordListViewHolder> {
    private List<AuthRecord> records;

    public AuthRecordListAdapter(List<AuthRecord> records) {
        this.records = records;
    }

    @NonNull
    @Override
    public AuthRecordListViewHolder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        TextView v = (TextView) LayoutInflater.from(parent.getContext())
                .inflate(R.layout.auth_record_text_view, parent, false);
        return new AuthRecordListViewHolder(v);
    }

    @Override
    public void onBindViewHolder(@NonNull AuthRecordListViewHolder holder, int position) {
        String str = SimpleDateFormat.getDateTimeInstance(SimpleDateFormat.LONG, SimpleDateFormat.LONG).format(records.get(position).getTime());
        holder.view.setText(str);
    }

    @Override
    public int getItemCount() {
        return records.size();
    }

    class AuthRecordListViewHolder extends RecyclerView.ViewHolder {
        TextView view;
        AuthRecordListViewHolder(TextView itemView) {
            super(itemView);
            view = itemView;
        }
    }
}
