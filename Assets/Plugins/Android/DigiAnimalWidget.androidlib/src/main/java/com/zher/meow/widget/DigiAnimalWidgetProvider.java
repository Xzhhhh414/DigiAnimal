package com.zher.meow.widget;

import android.app.PendingIntent;
import android.appwidget.AppWidgetManager;
import android.appwidget.AppWidgetProvider;
import android.content.Context;
import android.content.ComponentName;
import android.content.Intent;
import android.content.SharedPreferences;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.graphics.Canvas;
import android.graphics.Matrix;
import android.graphics.Paint;
import android.graphics.Typeface;
import android.os.Handler;
import android.os.Looper;
import android.util.Log;
import android.util.TypedValue;
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
    public static final String ACTION_REFRESH_DATA = "com.zher.meow.widget.REFRESH_DATA";
    public static final String ACTION_STOP_ANIMATION = "com.zher.meow.widget.STOP_ANIMATION";
    public static final String ACTION_PERIODIC_UPDATE = "com.zher.meow.widget.PERIODIC_UPDATE";
    
    // Intent额外参数
    public static final String EXTRA_WIDGET_ID = "widget_id";
    public static final String EXTRA_ANIMATION_TYPE = "animation_type";
    
    // SharedPreferences配置
    private static final String PREFS_NAME = "DigiAnimalWidgetData";
    private static final String KEY_WIDGET_DATA = "widget_data";
    private static final String KEY_WIDGET_STATE = "widget_state";
    
    // 动画状态
    private static final String STATE_SIT = "sit";
    private static final String STATE_LOOK = "look";
    private static final String STATE_RUN = "run";
    
    // 用于存储当前运行的动画Handler
    private static Handler currentAnimationHandler = null;
    
    @Override
    public void onUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds) {
        // Log.i(TAG, "=== onUpdate called with " + appWidgetIds.length + " widgets ===");
        
        // 更新所有小组件实例
        for (int widgetId : appWidgetIds) {
            // Log.d(TAG, "Updating widget ID: " + widgetId);
            updateWidget(context, appWidgetManager, widgetId);
        }
        
        // Log.i(TAG, "=== onUpdate completed ===");
    }
    
    @Override
    public void onEnabled(Context context) {
        super.onEnabled(context);
        // Log.i(TAG, "=== Widget ENABLED - First widget added to home screen ===");
        
        // 启动定期更新
        setupPeriodicUpdate(context);
    }

    @Override
    public void onDisabled(Context context) {
        super.onDisabled(context);
        // Log.i(TAG, "=== Widget DISABLED - Last widget removed from home screen ===");
        
        // 取消定期更新
        cancelPeriodicUpdate(context);
    }

    @Override
    public void onDeleted(Context context, int[] appWidgetIds) {
        super.onDeleted(context, appWidgetIds);
        // Log.i(TAG, "=== Widget DELETED - " + appWidgetIds.length + " widgets removed ===");
    }

    @Override
    public void onReceive(Context context, Intent intent) {
        super.onReceive(context, intent);
        // Log.d(TAG, "onReceive called with action: " + intent.getAction());
        
        String action = intent.getAction();
        // Log.d(TAG, "onReceive: " + action);
        
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
        } else if (ACTION_REFRESH_DATA.equals(action)) {
            // Log.d(TAG, "处理刷新数据请求 - 智能选择最佳数据源");
            
            // 使用数据提供者获取当前最佳数据（自动判断游戏数据vs离线数据）
            WidgetDataProvider dataProvider = WidgetDataProvider.getInstance(context);
            PetData refreshedData = dataProvider.getCurrentPetData();
            
            // 刷新所有小组件
            AppWidgetManager appWidgetManager = AppWidgetManager.getInstance(context);
            ComponentName provider = new ComponentName(context, DigiAnimalWidgetProvider.class);
            int[] widgetIds = appWidgetManager.getAppWidgetIds(provider);
            onUpdate(context, appWidgetManager, widgetIds);
            
            // Log.d(TAG, "小组件刷新完成 - 数据源: " + (refreshedData != null ? refreshedData.petName : "默认数据"));
        } else if (ACTION_PERIODIC_UPDATE.equals(action)) {
            // Log.d(TAG, "处理定期更新请求");
            
            // 使用数据提供者进行定期离线更新
            WidgetDataProvider dataProvider = WidgetDataProvider.getInstance(context);
            dataProvider.periodicOfflineUpdate();
            
            // 刷新所有小组件
            AppWidgetManager appWidgetManager = AppWidgetManager.getInstance(context);
            ComponentName provider = new ComponentName(context, DigiAnimalWidgetProvider.class);
            int[] widgetIds = appWidgetManager.getAppWidgetIds(provider);
            onUpdate(context, appWidgetManager, widgetIds);
            
            // Log.d(TAG, "定期更新完成");
        } else if ("com.zher.meow.widget.WIDGET_PINNED".equals(action)) {
            // 小组件固定成功回调
            // Log.i(TAG, "小组件固定成功回调");
            int appWidgetId = intent.getIntExtra("android.appwidget.extra.APPWIDGET_ID", -1);
            // Log.i(TAG, "收到的appWidgetId: " + appWidgetId);
            if (appWidgetId != -1) {
                // Log.i(TAG, "新固定的小组件ID: " + appWidgetId);
                // 立即更新新添加的小组件
                AppWidgetManager appWidgetManager = AppWidgetManager.getInstance(context);
                onUpdate(context, appWidgetManager, new int[]{appWidgetId});
                
                // 通知Unity小组件添加成功
                // Log.i(TAG, "准备调用notifyUnityWidgetAdded");
                notifyUnityWidgetAdded(context);
            } else {
                Log.w(TAG, "appWidgetId为-1，无法获取小组件ID，但仍然通知Unity");
                // 即使没有ID，也通知Unity添加成功
                notifyUnityWidgetAdded(context);
            }
        }
    }
    
    
    /**
     * 更新单个小组件
     */
    private void updateWidget(Context context, AppWidgetManager appWidgetManager, int widgetId) {
        // Log.d(TAG, "更新小组件: " + widgetId);
        
        // 创建RemoteViews
        RemoteViews views = new RemoteViews(context.getPackageName(), R.layout.digianimal_widget_4x2);
        
        // 使用新的数据提供者获取最佳数据源
        WidgetDataProvider dataProvider = WidgetDataProvider.getInstance(context);
        PetData petData = dataProvider.getCurrentPetData();
        
        if (petData != null && DataFreshnessChecker.isDataValid(petData)) {
            // 更新宠物信息
            updatePetInfo(context, views, petData);
            
            // 更新宠物图片
            updatePetImage(context, views, petData);
        } else {
            // 显示默认数据
            updateDefaultInfo(views, context);
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
     * 更新宠物信息文本 - 使用自定义字体位图
     */
    private void updatePetInfo(Context context, RemoteViews views, PetData petData) {
        // Log.d(TAG, "更新宠物信息: " + petData.petName);
        
        int textColor = 0xFFFFFFFF; // 白色
        // 宠物名称 - 使用文本位图
        Bitmap nameBitmap = createTextBitmap(context, petData.petName, 18, textColor);
        if (nameBitmap != null) {
            views.setImageViewBitmap(R.id.pet_name, nameBitmap);
        } else {
            views.setTextViewText(R.id.pet_name, petData.petName);
        }

        // 年龄 - 使用文本位图
        String ageText = String.format("年龄 %d天", petData.ageInDays);
        Bitmap ageBitmap = createTextBitmap(context, ageText, 16, textColor);
        if (ageBitmap != null) {
            views.setImageViewBitmap(R.id.pet_age, ageBitmap);
        } else {
            views.setTextViewText(R.id.pet_age, ageText);
        }
        
        // 精力 - 使用文本位图
        String energyText = String.format("精力 %d", petData.energy);
        Bitmap energyBitmap = createTextBitmap(context, energyText, 16, textColor);
        if (energyBitmap != null) {
            views.setImageViewBitmap(R.id.pet_energy, energyBitmap);
        } else {
            views.setTextViewText(R.id.pet_energy, energyText);
        }
        
        // 饱食度 - 使用文本位图
        String satietyText = String.format("饱食 %d", petData.satiety);
        Bitmap satietyBitmap = createTextBitmap(context, satietyText, 16, textColor);
        if (satietyBitmap != null) {
            views.setImageViewBitmap(R.id.pet_satiety, satietyBitmap);
        } else {
            views.setTextViewText(R.id.pet_satiety, satietyText);
        }
        
        // 宠物状态 - 根据优先级显示状态文本
        updatePetStatus(context, views, petData);
        
        // Log.d(TAG, "宠物信息更新完成: " + petData.petName + ", 年龄:" + petData.ageInDays + "天");
    }
    
    /**
     * 更新宠物图片 - 像素完美放大
     */
    private void updatePetImage(Context context, RemoteViews views, PetData petData) {
        int imageResId = PetImageHelper.getDefaultImageResource(context, petData.prefabName);
        Bitmap scaledBitmap = createPixelPerfectBitmap(context, imageResId);
        
        if (scaledBitmap != null) {
            views.setImageViewBitmap(R.id.pet_image, scaledBitmap);
            // Log.d(TAG, "使用像素完美放大的图片: " + scaledBitmap.getWidth() + "x" + scaledBitmap.getHeight());
        } else {
            // 备用方案：直接使用资源
            views.setImageViewResource(R.id.pet_image, imageResId);
            Log.w(TAG, "像素完美放大失败，使用原始资源");
        }
    }

    /**
     * 创建像素完美的放大位图
     */
    private Bitmap createPixelPerfectBitmap(Context context, int resourceId) {
        try {
            // 加载原始位图，禁用任何缩放
            BitmapFactory.Options options = new BitmapFactory.Options();
            options.inScaled = false; // 禁用自动缩放
            options.inDither = false; // 禁用抖动
            options.inPreferredConfig = Bitmap.Config.ARGB_8888; // 使用高质量配置
            
            Bitmap originalBitmap = BitmapFactory.decodeResource(context.getResources(), resourceId, options);
            if (originalBitmap == null) {
                return null;
            }
            
            // 使用 Matrix 进行整数倍放大
            int scaleFactor = 3; // 原始32x32
            Matrix matrix = new Matrix();
            matrix.setScale(scaleFactor, scaleFactor);
            
            // 创建放大后的位图
            Bitmap scaledBitmap = Bitmap.createBitmap(
                originalBitmap, 0, 0, 
                originalBitmap.getWidth(), originalBitmap.getHeight(), 
                matrix, false // 禁用滤镜，保持像素锐利
            );
            
            // Log.d(TAG, "像素完美放大: " + originalBitmap.getWidth() + "x" + originalBitmap.getHeight() + 
            //       " -> " + scaledBitmap.getWidth() + "x" + scaledBitmap.getHeight());
            
            return scaledBitmap;
        } catch (Exception e) {
            Log.e(TAG, "创建像素完美位图失败: " + e.getMessage());
            return null;
        }
    }
    
    /**
     * 显示默认信息
     */
    private void updateDefaultInfo(RemoteViews views, Context context) {
        views.setTextViewText(R.id.pet_age, "年龄 1天");
        views.setTextViewText(R.id.pet_energy, "精力 100");
        views.setTextViewText(R.id.pet_name, "我的宠物");
        views.setTextViewText(R.id.pet_satiety, "饱食 100");
        
        // 隐藏状态显示（默认状态良好）
        views.setViewVisibility(R.id.pet_status, android.view.View.GONE);
        
        // 使用像素完美放大的默认图片 (sit_1)
        int defaultFrame = PetImageHelper.getSingleFrame(context, "Pet_CatBrown", "sit_1");
        Bitmap scaledBitmap = createPixelPerfectBitmap(context, defaultFrame);
        if (scaledBitmap != null) {
            views.setImageViewBitmap(R.id.pet_image, scaledBitmap);
        } else {
            views.setImageViewResource(R.id.pet_image, defaultFrame);
        }
    }
    
    /**
     * 设置按钮点击事件和文本
     */
    private void setupButtonClickEvents(Context context, RemoteViews views, int widgetId) {
        // 获取当前状态
        String currentState = getWidgetState(context);
        
        // 设置按钮文本位图
        updateButtonText(context, views, R.id.btn_sit, "坐下", STATE_SIT.equals(currentState));
        updateButtonText(context, views, R.id.btn_visit, "左右看", STATE_LOOK.equals(currentState));
        updateButtonText(context, views, R.id.btn_run, "跑步", STATE_RUN.equals(currentState));
        
        // 坐下按钮
        Intent sitIntent = createAnimationIntent(context, widgetId, "sit");
        PendingIntent sitPendingIntent = PendingIntent.getBroadcast(
            context, widgetId * 10 + 1, sitIntent, 
            PendingIntent.FLAG_UPDATE_CURRENT | PendingIntent.FLAG_IMMUTABLE
        );
        views.setOnClickPendingIntent(R.id.btn_sit, sitPendingIntent);
        
        // 左右看按钮（随机左看右看）
        Intent lookIntent = createAnimationIntent(context, widgetId, "look");
        PendingIntent lookPendingIntent = PendingIntent.getBroadcast(
            context, widgetId * 10 + 2, lookIntent, 
            PendingIntent.FLAG_UPDATE_CURRENT | PendingIntent.FLAG_IMMUTABLE
        );
        views.setOnClickPendingIntent(R.id.btn_visit, lookPendingIntent);
        
        // 跑步按钮（循环播放）
        Intent runIntent = createAnimationIntent(context, widgetId, "run");
        PendingIntent runPendingIntent = PendingIntent.getBroadcast(
            context, widgetId * 10 + 3, runIntent, 
            PendingIntent.FLAG_UPDATE_CURRENT | PendingIntent.FLAG_IMMUTABLE
        );
        views.setOnClickPendingIntent(R.id.btn_run, runPendingIntent);
        
        // 刷新按钮
        Intent refreshIntent = new Intent(context, DigiAnimalWidgetProvider.class);
        refreshIntent.setAction(ACTION_REFRESH_DATA);
        PendingIntent refreshPendingIntent = PendingIntent.getBroadcast(
            context, widgetId * 10 + 4, refreshIntent, 
            PendingIntent.FLAG_UPDATE_CURRENT | PendingIntent.FLAG_IMMUTABLE
        );
        views.setOnClickPendingIntent(R.id.btn_refresh, refreshPendingIntent);
        
        // 小组件空白区域点击 - 启动游戏
        setupWidgetClickToLaunch(context, views, widgetId);
    }
    
    /**
     * 设置小组件点击启动游戏
     */
    private void setupWidgetClickToLaunch(Context context, RemoteViews views, int widgetId) {
        try {
            // 创建启动Unity游戏的Intent
            Intent launchIntent = context.getPackageManager().getLaunchIntentForPackage(context.getPackageName());
            if (launchIntent != null) {
                launchIntent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TOP);
                
                // 创建PendingIntent
                PendingIntent launchPendingIntent = PendingIntent.getActivity(
                    context, widgetId * 10 + 5, launchIntent, 
                    PendingIntent.FLAG_UPDATE_CURRENT | PendingIntent.FLAG_IMMUTABLE
                );
                
                // 为小组件的根布局设置点击事件
                views.setOnClickPendingIntent(R.id.widget_root, launchPendingIntent);
            }
        } catch (Exception e) {
            Log.e(TAG, "设置小组件点击启动失败: " + e.getMessage());
        }
    }
    
    /**
     * 更新按钮文本位图
     */
    private void updateButtonText(Context context, RemoteViews views, int buttonId, String text, boolean isSelected) {
        try {
            // 文本颜色统一为白色
            int textColor = 0xFFFFFFFF; // 统一使用白色
            
            // 创建文本位图 - 可以调整这里的数字来改变字体大小
            Bitmap textBitmap = createButtonTextBitmap(context, text, textColor, 16); // 字体大小：16sp（可调整）
            if (textBitmap != null) {
                views.setImageViewBitmap(buttonId, textBitmap);
                // Log.d(TAG, "设置按钮文本: " + text + ", 选中: " + isSelected + ", 位图尺寸: " + textBitmap.getWidth() + "x" + textBitmap.getHeight());
            } else {
                Log.e(TAG, "创建按钮文本位图失败，必须使用自定义字体: " + text);
            }
            
            // 设置按钮背景
            if (isSelected) {
                views.setInt(buttonId, "setBackgroundResource", R.drawable.widget_button_background_highlighted);
            } else {
                views.setInt(buttonId, "setBackgroundResource", R.drawable.widget_button_background);
            }
        } catch (Exception e) {
            Log.e(TAG, "设置按钮文本失败，必须修复自定义字体问题: " + text, e);
        }
    }
    
    /**
     * 创建按钮专用的文本位图（带背景以确保可见）
     */
    private Bitmap createButtonTextBitmap(Context context, String text, int textColor, int textSize) {
        try {
            // 加载自定义字体
            Typeface customFont = Typeface.createFromAsset(context.getAssets(), "fonts/ark_pixel_font_regular.ttf");
            
            // 创建Paint对象
            Paint paint = new Paint();
            paint.setTypeface(customFont);
            paint.setTextSize(TypedValue.applyDimension(TypedValue.COMPLEX_UNIT_SP, textSize, context.getResources().getDisplayMetrics()));
            paint.setColor(textColor);
            paint.setAntiAlias(false); // 像素字体不需要抗锯齿
            
            // 测量文本尺寸
            float textWidth = paint.measureText(text);
            Paint.FontMetrics fontMetrics = paint.getFontMetrics();
            float textHeight = fontMetrics.bottom - fontMetrics.top;
            
            // 创建位图，添加一些边距
            int bitmapWidth = (int) Math.ceil(textWidth) + 8; // 左右各4px边距
            int bitmapHeight = (int) Math.ceil(textHeight) + 8; // 上下各4px边距
            
            Bitmap bitmap = Bitmap.createBitmap(bitmapWidth, bitmapHeight, Bitmap.Config.ARGB_8888);
            Canvas canvas = new Canvas(bitmap);
            
            // 使用透明背景
            canvas.drawColor(0x00000000); // 完全透明背景
            
            // 绘制文本
            float x = 4; // 左边距
            float y = 4 - fontMetrics.top; // 基线位置
            canvas.drawText(text, x, y, paint);
            
            // Log.d(TAG, "创建按钮文本位图成功: " + text + ", 尺寸: " + bitmapWidth + "x" + bitmapHeight);
            return bitmap;
        } catch (Exception e) {
            Log.e(TAG, "创建按钮文本位图失败: " + text, e);
            return null;
        }
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
     * 通知Unity强制刷新数据
     */
    private void notifyUnityToRefreshData(Context context) {
        try {
            // Log.d(TAG, "通知Unity强制刷新数据");
            
            // 通过Intent启动Unity应用并传递刷新数据的指令
            Intent unityIntent = new Intent();
            unityIntent.setClassName(context.getPackageName(), "com.unity3d.player.UnityPlayerActivity");
            unityIntent.setAction("com.zher.meow.REFRESH_DATA");
            unityIntent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_SINGLE_TOP);
            
            // 添加自定义数据
            unityIntent.putExtra("refresh_source", "widget");
            unityIntent.putExtra("timestamp", System.currentTimeMillis());
            
            context.startActivity(unityIntent);
            
            // Log.i(TAG, "已发送刷新数据请求到Unity");
            
        } catch (Exception e) {
            Log.e(TAG, "通知Unity刷新数据失败", e);
        }
    }
    
    /**
     * 播放宠物动画
     */
    private void playAnimation(Context context, int widgetId, String animationType) {
        // Log.d(TAG, "切换到状态: " + animationType + " (Widget: " + widgetId + ")");
        
        // 停止当前动画
        stopCurrentAnimation();
        
        // 保存当前状态
        saveWidgetState(context, animationType);
        
        // 获取当前宠物数据
        WidgetData widgetData = loadWidgetData(context);
        if (widgetData == null || widgetData.selectedPetData == null) {
            Log.w(TAG, "无法播放动画：没有宠物数据");
            return;
        }
        
        if ("sit".equals(animationType)) {
            // 坐下状态：显示sit_1静态帧
            setState_Sit(context, widgetId, widgetData.selectedPetData.prefabName);
        } else if ("look".equals(animationType)) {
            // 左右看状态：播放左右看动画，然后保持在某个静态帧
            setState_Look(context, widgetId, widgetData.selectedPetData.prefabName);
        } else if ("run".equals(animationType)) {
            // 跑步状态：循环播放跑步动画
            setState_Run(context, widgetId, widgetData.selectedPetData.prefabName);
        }
        
        // 更新所有按钮状态
        updateAllButtonStates(context, widgetId);
    }
    
    /**
     * 更新所有按钮的状态
     */
    private void updateAllButtonStates(Context context, int widgetId) {
        try {
            AppWidgetManager appWidgetManager = AppWidgetManager.getInstance(context);
            RemoteViews views = new RemoteViews(context.getPackageName(), R.layout.digianimal_widget_4x2);
            
            // 获取当前状态
            String currentState = getWidgetState(context);
            
            // 更新按钮文本和状态
            updateButtonText(context, views, R.id.btn_sit, "坐下", STATE_SIT.equals(currentState));
            updateButtonText(context, views, R.id.btn_visit, "左右看", STATE_LOOK.equals(currentState));
            updateButtonText(context, views, R.id.btn_run, "跑步", STATE_RUN.equals(currentState));
            
            // 只更新按钮，不重新设置点击事件
            appWidgetManager.partiallyUpdateAppWidget(widgetId, views);
            // Log.d(TAG, "按钮状态已更新，当前状态: " + currentState);
        } catch (Exception e) {
            Log.e(TAG, "更新按钮状态失败", e);
        }
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
                    
                    // 设置当前动画帧（像素完美放大）
                    Bitmap scaledFrame = createPixelPerfectBitmap(context, frameResource);
                    if (scaledFrame != null) {
                        views.setImageViewBitmap(R.id.pet_image, scaledFrame);
                    } else {
                        views.setImageViewResource(R.id.pet_image, frameResource);
                    }
                    
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
     * 将像素风位图按整数倍最近邻放大，避免模糊
     */
    private Bitmap scalePixelArtBitmap(Context context, Bitmap src) {
        if (src == null) return null;
        // 以基准目标尺寸128dp为例，转成像素后计算最接近的整数倍
        int targetPx = (int) TypedValue.applyDimension(TypedValue.COMPLEX_UNIT_DIP, 128, context.getResources().getDisplayMetrics());
        int w = src.getWidth();
        int h = src.getHeight();
        if (w <= 0 || h <= 0) return src;
        int scaleW = Math.max(1, targetPx / w);
        int scaleH = Math.max(1, targetPx / h);
        int scale = Math.max(1, Math.min(scaleW, scaleH));
        // 整数倍放大
        int dstW = w * scale;
        int dstH = h * scale;
        return Bitmap.createScaledBitmap(src, dstW, dstH, false);
    }

    /**
     * 更新宠物状态显示
     * 优先级：精力≤10 > 饱食≤10 > isBored=true
     */
    private void updatePetStatus(Context context, RemoteViews views, PetData petData) {
        String statusText = null;
        
        // 按优先级判断状态
        if (petData.energy <= 10) {
            statusText = "好困…";
        } else if (petData.satiety <= 10) {
            statusText = "好饿…";
        } else if (petData.isBored) {
            statusText = "玩累了…";
        }
        
        if (statusText != null) {
            // 显示状态文本（红色，字号14）
            int redColor = 0xFFFF0000; // 红色
            Bitmap statusBitmap = createTextBitmap(context, statusText, 14, redColor);
            if (statusBitmap != null) {
                views.setImageViewBitmap(R.id.pet_status, statusBitmap);
                views.setViewVisibility(R.id.pet_status, android.view.View.VISIBLE);
            } else {
                views.setViewVisibility(R.id.pet_status, android.view.View.GONE);
            }
        } else {
            // 没有状态需要显示，隐藏状态区域
            views.setViewVisibility(R.id.pet_status, android.view.View.GONE);
        }
    }

    /**
     * 创建自定义字体的文本位图
     */
    private Bitmap createTextBitmap(Context context, String text, int textSizeSp, int textColor) {
        try {
            // 加载自定义字体
            Typeface customTypeface = Typeface.createFromAsset(context.getAssets(), "fonts/ark_pixel_font_regular.ttf");
            
            // 设置画笔
            Paint paint = new Paint(Paint.ANTI_ALIAS_FLAG);
            paint.setTypeface(customTypeface);
            paint.setTextSize(TypedValue.applyDimension(TypedValue.COMPLEX_UNIT_SP, textSizeSp, context.getResources().getDisplayMetrics()));
            paint.setColor(textColor);
            
            // 测量文本尺寸
            Paint.FontMetrics fontMetrics = paint.getFontMetrics();
            int textWidth = (int) (paint.measureText(text) + 0.5f);
            int textHeight = (int) (fontMetrics.bottom - fontMetrics.top + 0.5f);
            
            // 创建位图
            Bitmap bitmap = Bitmap.createBitmap(textWidth, textHeight, Bitmap.Config.ARGB_8888);
            Canvas canvas = new Canvas(bitmap);
            
            // 绘制文本
            float x = 0;
            float y = -fontMetrics.top;
            canvas.drawText(text, x, y, paint);
            
            return bitmap;
        } catch (Exception e) {
            Log.e(TAG, "创建文本位图失败: " + e.getMessage());
            return null;
        }
    }
    
    /**
     * 显示单个帧
     */
    private void showSingleFrame(Context context, int widgetId, int frameResource) {
        AppWidgetManager appWidgetManager = AppWidgetManager.getInstance(context);
        RemoteViews views = new RemoteViews(context.getPackageName(), R.layout.digianimal_widget_4x2);
        
        // 保持其他信息不变，只更新图片
        WidgetData widgetData = loadWidgetData(context);
        if (widgetData != null && widgetData.selectedPetData != null) {
            updatePetInfo(context, views, widgetData.selectedPetData);
            setupButtonClickEvents(context, views, widgetId);
        }
        
        // 设置指定帧
        Bitmap scaledFrame = createPixelPerfectBitmap(context, frameResource);
        if (scaledFrame != null) {
            views.setImageViewBitmap(R.id.pet_image, scaledFrame);
        } else {
            views.setImageViewResource(R.id.pet_image, frameResource);
        }
        
        appWidgetManager.updateAppWidget(widgetId, views);
    }
    
    
    /**
     * 停止当前动画
     */
    private void stopCurrentAnimation() {
        if (currentAnimationHandler != null) {
            currentAnimationHandler.removeCallbacksAndMessages(null);
            currentAnimationHandler = null;
            // Log.d(TAG, "已停止当前动画");
        }
    }
    
    /**
     * 保存小组件状态
     */
    private void saveWidgetState(Context context, String state) {
        SharedPreferences prefs = context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE);
        prefs.edit().putString(KEY_WIDGET_STATE, state).apply();
        // Log.d(TAG, "保存状态: " + state);
    }
    
    /**
     * 获取小组件状态
     */
    private String getWidgetState(Context context) {
        SharedPreferences prefs = context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE);
        return prefs.getString(KEY_WIDGET_STATE, STATE_SIT); // 默认坐下状态
    }
    
    /**
     * 设置坐下状态
     */
    private void setState_Sit(Context context, int widgetId, String prefabName) {
        int sitFrame = PetImageHelper.getSingleFrame(context, prefabName, "sit_1");
        if (sitFrame != 0) {
            showSingleFrame(context, widgetId, sitFrame);
            // Log.d(TAG, "切换到坐下状态");
        }
    }
    
    /**
     * 设置左右看状态（循环模式）
     */
    private void setState_Look(Context context, int widgetId, String prefabName) {
        currentAnimationHandler = new Handler(Looper.getMainLooper());
        
        // 获取所有需要的动画帧
        int lookleft_1 = PetImageHelper.getSingleFrame(context, prefabName, "lookleft_1");
        int lookleft_2 = PetImageHelper.getSingleFrame(context, prefabName, "lookleft_2");
        int lookright_1 = PetImageHelper.getSingleFrame(context, prefabName, "lookright_1");
        int lookright_2 = PetImageHelper.getSingleFrame(context, prefabName, "lookright_2");
        
        if (lookleft_1 == 0 || lookleft_2 == 0 || lookright_1 == 0 || lookright_2 == 0) {
            Log.w(TAG, "找不到左右看动画帧，使用坐下状态");
            setState_Sit(context, widgetId, prefabName);
            return;
        }
        
        // 存储动画帧数组，用于随机选择
        int[][] lookAnimations = {
            {lookleft_1, lookleft_2},   // 左看动画
            {lookright_1, lookright_2}  // 右看动画
        };
        
        // 开始左右看循环
        startLookCycle(context, widgetId, lookAnimations);
        // Log.d(TAG, "开始左右看循环动画");
    }
    
    /**
     * 开始左右看循环
     */
    private void startLookCycle(Context context, int widgetId, int[][] lookAnimations) {
        if (currentAnimationHandler == null) return; // 动画已被停止
        
        // 随机选择左看或右看（0=左看，1=右看）
        int randomDirection = (int)(Math.random() * 2);
        int[] selectedAnimation = lookAnimations[randomDirection];
        String direction = randomDirection == 0 ? "左看" : "右看";
        
        // Log.d(TAG, "播放" + direction + "动画");
        
        // 播放选中的动画：frame1 -> frame2 -> frame1
        int[] sequence = {selectedAnimation[0], selectedAnimation[1], selectedAnimation[0]};
        int frameInterval = 30; // 每帧30ms
        
        // 播放动画序列
        for (int i = 0; i < sequence.length; i++) {
            final int frameResource = sequence[i];
            final boolean isLastFrame = (i == sequence.length - 1);
            
            currentAnimationHandler.postDelayed(new Runnable() {
                @Override
                public void run() {
                    if (currentAnimationHandler == null) return; // 检查动画是否被停止
                    
                    showSingleFrame(context, widgetId, frameResource);
                    
                    // 最后一帧后等待随机时间，然后继续下一个循环
                    if (isLastFrame) {
                        scheduleNextLookCycle(context, widgetId, lookAnimations);
                    }
                }
            }, i * frameInterval);
        }
    }
    
    /**
     * 安排下一个左右看循环
     */
    private void scheduleNextLookCycle(Context context, int widgetId, int[][] lookAnimations) {
        if (currentAnimationHandler == null) return; // 动画已被停止
        
        // 随机等待时间：1-2秒
        int waitTime = (int)(1000 + Math.random() * 1000); // 1000-2000ms
        // Log.d(TAG, "等待" + waitTime + "ms后播放下一个左右看动画");
        
        currentAnimationHandler.postDelayed(new Runnable() {
            @Override
            public void run() {
                startLookCycle(context, widgetId, lookAnimations);
            }
        }, waitTime);
    }
    
    /**
     * 设置跑步状态
     */
    private void setState_Run(Context context, int widgetId, String prefabName) {
        // 获取跑步动画帧
        int run1 = PetImageHelper.getSingleFrame(context, prefabName, "run_1");
        int run2 = PetImageHelper.getSingleFrame(context, prefabName, "run_2");
        int run3 = PetImageHelper.getSingleFrame(context, prefabName, "run_3");
        int run4 = PetImageHelper.getSingleFrame(context, prefabName, "run_4");
        
        if (run1 == 0 || run2 == 0 || run3 == 0 || run4 == 0) {
            Log.w(TAG, "找不到跑步动画帧，使用坐下状态");
            setState_Sit(context, widgetId, prefabName);
            return;
        }
        
        int[] runFrames = {run1, run2, run3, run4};
        currentAnimationHandler = new Handler(Looper.getMainLooper());
        final int[] currentFrame = {0};
        
        Runnable runAnimation = new Runnable() {
            @Override
            public void run() {
                if (currentAnimationHandler == null) return; // 动画已被停止
                
                showSingleFrame(context, widgetId, runFrames[currentFrame[0]]);
                currentFrame[0] = (currentFrame[0] + 1) % runFrames.length;
                
                // 继续下一帧
                currentAnimationHandler.postDelayed(this, 5); // 动画播放速度
            }
        };
        
        // 开始循环动画
        currentAnimationHandler.post(runAnimation);
        // Log.d(TAG, "开始跑步循环动画");
    }
    
    /**
     * 恢复默认状态
     */
    private void restoreDefaultState(Context context, AppWidgetManager appWidgetManager, int widgetId) {
        // Log.d(TAG, "恢复默认状态: " + widgetId);
        
        // 停止当前动画
        stopCurrentAnimation();
        
        // 获取保存的状态，如果没有则默认为坐下
        String currentState = getWidgetState(context);
        
        WidgetData widgetData = loadWidgetData(context);
        if (widgetData != null && widgetData.selectedPetData != null) {
            // 根据保存的状态恢复
            if (STATE_RUN.equals(currentState)) {
                setState_Run(context, widgetId, widgetData.selectedPetData.prefabName);
            } else if (STATE_LOOK.equals(currentState)) {
                // 左右看状态恢复为静态的左看或右看帧
                setState_Sit(context, widgetId, widgetData.selectedPetData.prefabName);
            } else {
                setState_Sit(context, widgetId, widgetData.selectedPetData.prefabName);
            }
            
            // 更新按钮状态
            updateAllButtonStates(context, widgetId);
        } else {
            // 备用方案
            updateWidget(context, appWidgetManager, widgetId);
        }
    }
    
    /**
     * 设置定期更新（每分钟）
     */
    private void setupPeriodicUpdate(Context context) {
        try {
            android.app.AlarmManager alarmManager = (android.app.AlarmManager) context.getSystemService(Context.ALARM_SERVICE);
            if (alarmManager == null) {
                Log.e(TAG, "无法获取AlarmManager");
                return;
            }
            
            Intent intent = new Intent(context, DigiAnimalWidgetProvider.class);
            intent.setAction(ACTION_PERIODIC_UPDATE);
            
            android.app.PendingIntent pendingIntent = android.app.PendingIntent.getBroadcast(
                context, 0, intent, 
                android.app.PendingIntent.FLAG_UPDATE_CURRENT | android.app.PendingIntent.FLAG_IMMUTABLE
            );
            
            // 每分钟更新一次 (60 * 1000 = 60000毫秒)
            long intervalMillis = 60 * 1000;
            long firstTimeMillis = System.currentTimeMillis() + intervalMillis;
            
            // 使用setRepeating设置重复闹钟
            alarmManager.setRepeating(
                android.app.AlarmManager.RTC_WAKEUP,
                firstTimeMillis,
                intervalMillis,
                pendingIntent
            );
            
            Log.d(TAG, "定期更新已设置: 每分钟更新一次");
            
        } catch (Exception e) {
            Log.e(TAG, "设置定期更新失败: " + e.getMessage());
        }
    }
    
    /**
     * 取消定期更新
     */
    private void cancelPeriodicUpdate(Context context) {
        try {
            android.app.AlarmManager alarmManager = (android.app.AlarmManager) context.getSystemService(Context.ALARM_SERVICE);
            if (alarmManager == null) {
                Log.e(TAG, "无法获取AlarmManager");
                return;
            }
            
            Intent intent = new Intent(context, DigiAnimalWidgetProvider.class);
            intent.setAction(ACTION_PERIODIC_UPDATE);
            
            android.app.PendingIntent pendingIntent = android.app.PendingIntent.getBroadcast(
                context, 0, intent, 
                android.app.PendingIntent.FLAG_UPDATE_CURRENT | android.app.PendingIntent.FLAG_IMMUTABLE
            );
            
            alarmManager.cancel(pendingIntent);
            pendingIntent.cancel();
            
            Log.d(TAG, "定期更新已取消");
            
        } catch (Exception e) {
            Log.e(TAG, "取消定期更新失败: " + e.getMessage());
        }
    }
    
    /**
     * 通知Unity小组件添加成功
     */
    private void notifyUnityWidgetAdded(Context context) {
        try {
            // Log.i(TAG, "准备通知Unity小组件添加成功");
            
            // 使用固定的GameObject名称发送Unity消息
            Class<?> unityPlayerClass = Class.forName("com.unity3d.player.UnityPlayer");
            java.lang.reflect.Method unitySendMessageMethod = unityPlayerClass.getMethod(
                "UnitySendMessage", String.class, String.class, String.class
            );
            
            // 发送到固定的GameObject：WidgetCallbackReceiver
            unitySendMessageMethod.invoke(null, 
                "WidgetCallbackReceiver", 
                "OnWidgetAddedSuccess", 
                "Widget added successfully"
            );
            
            // Log.i(TAG, "Unity消息发送成功到: WidgetCallbackReceiver");
            
        } catch (Exception e) {
            Log.e(TAG, "通知Unity失败: " + e.getMessage());
            e.printStackTrace();
        }
    }
}
