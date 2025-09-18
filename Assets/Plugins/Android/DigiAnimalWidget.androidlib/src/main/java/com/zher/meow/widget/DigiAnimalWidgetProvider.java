package com.zher.meow.widget;

import android.app.PendingIntent;
import android.appwidget.AppWidgetManager;
import android.appwidget.AppWidgetProvider;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.os.Handler;
import android.os.Looper;
import android.util.Log;
import android.widget.RemoteViews;

import org.json.JSONException;
import org.json.JSONObject;

/**
 * DigiAnimal桌面小组件Provider
 * 处理小组件的更新、点击事件和动画播放
 */
public class DigiAnimalWidgetProvider extends AppWidgetProvider {
    
    private static final String TAG = "DigiAnimalWidget";
    
    // 自定义Action
    public static final String ACTION_PLAY_ANIMATION = "com.zher.meow.widget.PLAY_ANIMATION";
    public static final String ACTION_REFRESH_WIDGET = "com.zher.meow.widget.REFRESH_WIDGET";
    
    // Intent额外参数
    public static final String EXTRA_WIDGET_ID = "widget_id";
    public static final String EXTRA_ANIMATION_TYPE = "animation_type";
    
    // SharedPreferences配置
    private static final String PREFS_NAME = "DigiAnimalWidgetData";
    private static final String KEY_WIDGET_DATA = "widget_data";
    
    @Override
    public void onUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds) {
        Log.i(TAG, "=== onUpdate called with " + appWidgetIds.length + " widgets ===");
        
        // 更新所有小组件实例
        for (int widgetId : appWidgetIds) {
            Log.d(TAG, "Updating widget ID: " + widgetId);
            updateWidget(context, appWidgetManager, widgetId);
        }
        
        Log.i(TAG, "=== onUpdate completed ===");
    }
    
    @Override
    public void onEnabled(Context context) {
        super.onEnabled(context);
        Log.i(TAG, "=== Widget ENABLED - First widget added to home screen ===");
    }

    @Override
    public void onDisabled(Context context) {
        super.onDisabled(context);
        Log.i(TAG, "=== Widget DISABLED - Last widget removed from home screen ===");
    }

    @Override
    public void onDeleted(Context context, int[] appWidgetIds) {
        super.onDeleted(context, appWidgetIds);
        Log.i(TAG, "=== Widget DELETED - " + appWidgetIds.length + " widgets removed ===");
    }

    @Override
    public void onReceive(Context context, Intent intent) {
        super.onReceive(context, intent);
        Log.d(TAG, "onReceive called with action: " + intent.getAction());
        
        String action = intent.getAction();
        Log.d(TAG, "onReceive: " + action);
        
        if (ACTION_PLAY_ANIMATION.equals(action)) {
            // 播放动画
            int widgetId = intent.getIntExtra(EXTRA_WIDGET_ID, -1);
            String animationType = intent.getStringExtra(EXTRA_ANIMATION_TYPE);
            
            if (widgetId != -1 && animationType != null) {
                playAnimation(context, widgetId, animationType);
            }
        } else if (ACTION_REFRESH_WIDGET.equals(action)) {
            // 刷新小组件
            int widgetId = intent.getIntExtra(EXTRA_WIDGET_ID, -1);
            if (widgetId != -1) {
                AppWidgetManager appWidgetManager = AppWidgetManager.getInstance(context);
                updateWidget(context, appWidgetManager, widgetId);
            }
        }
    }
    
    
    /**
     * 更新单个小组件
     */
    private void updateWidget(Context context, AppWidgetManager appWidgetManager, int widgetId) {
        Log.d(TAG, "更新小组件: " + widgetId);
        
        // 创建RemoteViews
        RemoteViews views = new RemoteViews(context.getPackageName(), R.layout.digianimal_widget_4x2);
        
        // 从SharedPreferences读取宠物数据
        WidgetData widgetData = loadWidgetData(context);
        
        if (widgetData != null && widgetData.selectedPetData != null) {
            // 更新宠物信息
            updatePetInfo(context, views, widgetData.selectedPetData);
            
            // 更新宠物图片
            updatePetImage(context, views, widgetData.selectedPetData);
        } else {
            // 显示默认数据
            updateDefaultInfo(views);
        }
        
        // 设置按钮点击事件
        setupButtonClickEvents(context, views, widgetId);
        
        // 更新小组件
        appWidgetManager.updateAppWidget(widgetId, views);
    }
    
    /**
     * 从SharedPreferences加载小组件数据
     */
    private WidgetData loadWidgetData(Context context) {
        SharedPreferences prefs = context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE);
        String jsonData = prefs.getString(KEY_WIDGET_DATA, null);
        
        if (jsonData == null) {
            Log.w(TAG, "没有找到小组件数据");
            return null;
        }
        
        try {
            return WidgetData.fromJson(jsonData);
        } catch (JSONException e) {
            Log.e(TAG, "解析小组件数据失败", e);
            return null;
        }
    }
    
    /**
     * 更新宠物信息文本
     */
    private void updatePetInfo(Context context, RemoteViews views, PetData petData) {
        // 年龄
        String ageText = String.format("年龄 %d天", petData.ageInDays);
        views.setTextViewText(R.id.pet_age, ageText);
        
        // 精力
        String energyText = String.format("精力 %d", petData.energy);
        views.setTextViewText(R.id.pet_energy, energyText);
        
        // 宠物名称
        views.setTextViewText(R.id.pet_name, petData.petName);
        
        // 饱食度
        String satietyText = String.format("饱食 %d", petData.satiety);
        views.setTextViewText(R.id.pet_satiety, satietyText);
    }
    
    /**
     * 更新宠物图片
     */
    private void updatePetImage(Context context, RemoteViews views, PetData petData) {
        // 根据prefabName获取对应的图片资源
        int imageResId = PetImageHelper.getDefaultImageResource(context, petData.prefabName);
        views.setImageViewResource(R.id.pet_image, imageResId);
    }
    
    /**
     * 显示默认信息
     */
    private void updateDefaultInfo(RemoteViews views) {
        views.setTextViewText(R.id.pet_age, "年龄 1天");
        views.setTextViewText(R.id.pet_energy, "精力 80");
        views.setTextViewText(R.id.pet_name, "我的宠物");
        views.setTextViewText(R.id.pet_satiety, "饱食 70");
        views.setImageViewResource(R.id.pet_image, R.drawable.pet_catbrown_sit_1);
    }
    
    /**
     * 设置按钮点击事件
     */
    private void setupButtonClickEvents(Context context, RemoteViews views, int widgetId) {
        // 坐下按钮
        Intent sitIntent = createAnimationIntent(context, widgetId, "sit");
        PendingIntent sitPendingIntent = PendingIntent.getBroadcast(
            context, widgetId * 10 + 1, sitIntent, 
            PendingIntent.FLAG_UPDATE_CURRENT | PendingIntent.FLAG_IMMUTABLE
        );
        views.setOnClickPendingIntent(R.id.btn_sit, sitPendingIntent);
        
        // 去看看按钮（趴下动画）
        Intent visitIntent = createAnimationIntent(context, widgetId, "laydown");
        PendingIntent visitPendingIntent = PendingIntent.getBroadcast(
            context, widgetId * 10 + 2, visitIntent, 
            PendingIntent.FLAG_UPDATE_CURRENT | PendingIntent.FLAG_IMMUTABLE
        );
        views.setOnClickPendingIntent(R.id.btn_visit, visitPendingIntent);
        
        // 跑步按钮
        Intent runIntent = createAnimationIntent(context, widgetId, "run");
        PendingIntent runPendingIntent = PendingIntent.getBroadcast(
            context, widgetId * 10 + 3, runIntent, 
            PendingIntent.FLAG_UPDATE_CURRENT | PendingIntent.FLAG_IMMUTABLE
        );
        views.setOnClickPendingIntent(R.id.btn_run, runPendingIntent);
    }
    
    /**
     * 创建动画播放Intent
     */
    private Intent createAnimationIntent(Context context, int widgetId, String animationType) {
        Intent intent = new Intent(context, DigiAnimalWidgetProvider.class);
        intent.setAction(ACTION_PLAY_ANIMATION);
        intent.putExtra(EXTRA_WIDGET_ID, widgetId);
        intent.putExtra(EXTRA_ANIMATION_TYPE, animationType);
        return intent;
    }
    
    /**
     * 播放宠物动画
     */
    private void playAnimation(Context context, int widgetId, String animationType) {
        Log.d(TAG, "播放动画: " + animationType + " (Widget: " + widgetId + ")");
        
        // 获取当前宠物数据
        WidgetData widgetData = loadWidgetData(context);
        if (widgetData == null || widgetData.selectedPetData == null) {
            Log.w(TAG, "无法播放动画：没有宠物数据");
            return;
        }
        
        // 获取动画帧资源
        int[] animationFrames = PetImageHelper.getAnimationFrames(
            context, widgetData.selectedPetData.prefabName, animationType
        );
        
        if (animationFrames.length == 0) {
            Log.w(TAG, "无法播放动画：没有找到动画帧");
            return;
        }
        
        // 播放帧序列动画
        playFrameSequence(context, widgetId, animationFrames);
    }
    
    /**
     * 播放帧序列动画
     */
    private void playFrameSequence(Context context, int widgetId, int[] frames) {
        AppWidgetManager appWidgetManager = AppWidgetManager.getInstance(context);
        Handler handler = new Handler(Looper.getMainLooper());
        
        final int frameInterval = 150; // 每帧150ms
        
        for (int i = 0; i < frames.length; i++) {
            final int frameIndex = i;
            final int frameResource = frames[i];
            
            handler.postDelayed(new Runnable() {
                @Override
                public void run() {
                    // 更新当前帧
                    RemoteViews views = new RemoteViews(context.getPackageName(), R.layout.digianimal_widget_4x2);
                    
                    // 保持其他信息不变，只更新图片
                    WidgetData widgetData = loadWidgetData(context);
                    if (widgetData != null && widgetData.selectedPetData != null) {
                        updatePetInfo(context, views, widgetData.selectedPetData);
                        setupButtonClickEvents(context, views, widgetId);
                    }
                    
                    // 设置当前动画帧
                    views.setImageViewResource(R.id.pet_image, frameResource);
                    
                    // 更新小组件
                    appWidgetManager.updateAppWidget(widgetId, views);
                    
                    // 如果是最后一帧，恢复默认状态
                    if (frameIndex == frames.length - 1) {
                        handler.postDelayed(new Runnable() {
                            @Override
                            public void run() {
                                restoreDefaultState(context, appWidgetManager, widgetId);
                            }
                        }, frameInterval);
                    }
                }
            }, i * frameInterval);
        }
    }
    
    /**
     * 恢复默认状态
     */
    private void restoreDefaultState(Context context, AppWidgetManager appWidgetManager, int widgetId) {
        Log.d(TAG, "恢复默认状态: " + widgetId);
        updateWidget(context, appWidgetManager, widgetId);
    }
}
