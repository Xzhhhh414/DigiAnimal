package com.zher.meow.widget;

import android.app.Activity;
import android.appwidget.AppWidgetManager;
import android.content.Intent;
import android.os.Bundle;
import android.util.Log;

/**
 * 小组件配置Activity
 * 当用户添加小组件时会启动此Activity（如果在widget_info.xml中配置了）
 */
public class WidgetConfigActivity extends Activity {
    
    private static final String TAG = "WidgetConfigActivity";
    
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        Log.i(TAG, "=== Widget Configuration Activity Started ===");
        
        // 获取小组件ID
        Intent intent = getIntent();
        Bundle extras = intent.getExtras();
        int appWidgetId = AppWidgetManager.INVALID_APPWIDGET_ID;
        
        if (extras != null) {
            appWidgetId = extras.getInt(
                AppWidgetManager.EXTRA_APPWIDGET_ID, 
                AppWidgetManager.INVALID_APPWIDGET_ID
            );
        }
        
        Log.d(TAG, "Widget ID: " + appWidgetId);
        
        // 由于我们的小组件不需要用户配置，直接完成配置
        Intent resultValue = new Intent();
        resultValue.putExtra(AppWidgetManager.EXTRA_APPWIDGET_ID, appWidgetId);
        setResult(RESULT_OK, resultValue);
        
        Log.i(TAG, "=== Widget Configuration Completed ===");
        
        // 立即结束Activity
        finish();
    }
}
