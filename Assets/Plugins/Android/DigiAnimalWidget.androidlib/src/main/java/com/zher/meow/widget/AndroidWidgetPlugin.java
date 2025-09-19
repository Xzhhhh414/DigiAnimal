package com.zher.meow.widget;

import android.app.Activity;
import android.appwidget.AppWidgetManager;
import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.os.Build;
import android.util.Log;

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

/**
 * Unity-Android通信插件
 * 提供Unity调用Android小组件功能的接口
 */
public class AndroidWidgetPlugin {
    
    private static final String TAG = "AndroidWidgetPlugin";
    private static final String PREFS_NAME = "DigiAnimalWidgetData";
    private static final String KEY_WIDGET_DATA = "widget_data";
    
    private Context context;
    
    public AndroidWidgetPlugin(Activity activity) {
        this.context = activity.getApplicationContext();
        Log.d(TAG, "AndroidWidgetPlugin初始化完成");
        
        // 验证资源完整性
        PetImageHelper.validateResources(context);
    }
    
    /**
     * 更新小组件数据
     * 由Unity调用，传入JSON格式的宠物数据
     */
    public void updateWidgetData(String jsonData) {
        Log.d(TAG, "更新小组件数据: " + jsonData);
        
        try {
            // 验证JSON数据
            WidgetData widgetData = WidgetData.fromJson(jsonData);
            Log.d(TAG, "解析的数据: " + widgetData.toString());
            
            if (widgetData.selectedPetData != null) {
                PetData petData = widgetData.selectedPetData;
                Log.d(TAG, "宠物数据 - 名字: " + petData.petName + 
                          ", 精力: " + petData.energy + 
                          ", 饱食: " + petData.satiety + 
                          ", 年龄: " + petData.ageInDays + "天");
                
                // 使用新的数据提供者处理游戏数据更新
                WidgetDataProvider dataProvider = WidgetDataProvider.getInstance(context);
                dataProvider.handleGameDataUpdate(petData);
            } else {
                Log.w(TAG, "小组件数据中没有宠物数据");
            }
            
            // 保存数据到SharedPreferences (保留兼容性)
            saveWidgetData(jsonData);
            
            // 刷新所有小组件
            refreshAllWidgets();
            
            Log.i(TAG, "小组件数据更新成功");
            
        } catch (JSONException e) {
            Log.e(TAG, "更新小组件数据失败: JSON解析错误", e);
        } catch (Exception e) {
            Log.e(TAG, "更新小组件数据失败", e);
        }
    }
    
    /**
     * 播放宠物动画
     * 由Unity调用，或者从小组件按钮触发
     */
    public void playPetAnimation(String petPrefabName, String animationType) {
        Log.d(TAG, "播放宠物动画: " + petPrefabName + " - " + animationType);
        
        try {
            // 获取所有小组件ID
            AppWidgetManager appWidgetManager = AppWidgetManager.getInstance(context);
            ComponentName provider = new ComponentName(context, DigiAnimalWidgetProvider.class);
            int[] widgetIds = appWidgetManager.getAppWidgetIds(provider);
            
            if (widgetIds.length == 0) {
                Log.w(TAG, "没有找到小组件实例");
                return;
            }
            
            // 为所有小组件播放动画
            for (int widgetId : widgetIds) {
                Intent intent = new Intent(context, DigiAnimalWidgetProvider.class);
                intent.setAction(DigiAnimalWidgetProvider.ACTION_PLAY_ANIMATION);
                intent.putExtra(DigiAnimalWidgetProvider.EXTRA_WIDGET_ID, widgetId);
                intent.putExtra(DigiAnimalWidgetProvider.EXTRA_ANIMATION_TYPE, animationType);
                
                context.sendBroadcast(intent);
            }
            
            Log.i(TAG, "动画播放请求已发送到 " + widgetIds.length + " 个小组件");
            
        } catch (Exception e) {
            Log.e(TAG, "播放动画失败", e);
        }
    }
    
    /**
     * 刷新所有小组件
     */
    public void refreshAllWidgets() {
        Log.d(TAG, "刷新所有小组件");
        
        try {
            AppWidgetManager appWidgetManager = AppWidgetManager.getInstance(context);
            ComponentName provider = new ComponentName(context, DigiAnimalWidgetProvider.class);
            int[] widgetIds = appWidgetManager.getAppWidgetIds(provider);
            
            if (widgetIds.length == 0) {
                Log.w(TAG, "没有找到小组件实例");
                return;
            }
            
            // 触发小组件更新
            Intent intent = new Intent(context, DigiAnimalWidgetProvider.class);
            intent.setAction(AppWidgetManager.ACTION_APPWIDGET_UPDATE);
            intent.putExtra(AppWidgetManager.EXTRA_APPWIDGET_IDS, widgetIds);
            
            context.sendBroadcast(intent);
            
            Log.i(TAG, "已刷新 " + widgetIds.length + " 个小组件");
            
        } catch (Exception e) {
            Log.e(TAG, "刷新小组件失败", e);
        }
    }
    
    /**
     * 检查小组件是否支持
     */
    public boolean isWidgetSupported() {
        try {
            AppWidgetManager appWidgetManager = AppWidgetManager.getInstance(context);
            return appWidgetManager != null;
        } catch (Exception e) {
            Log.e(TAG, "检查小组件支持失败", e);
            return false;
        }
    }
    
    /**
     * 获取设备信息
     */
    public String getDeviceInfo() {
        try {
            JSONObject deviceInfo = new JSONObject();
            deviceInfo.put("manufacturer", Build.MANUFACTURER);
            deviceInfo.put("model", Build.MODEL);
            deviceInfo.put("apiLevel", Build.VERSION.SDK_INT);
            deviceInfo.put("androidVersion", Build.VERSION.RELEASE);
            deviceInfo.put("brand", Build.BRAND);
            
            // 获取小组件数量
            AppWidgetManager appWidgetManager = AppWidgetManager.getInstance(context);
            ComponentName provider = new ComponentName(context, DigiAnimalWidgetProvider.class);
            int[] widgetIds = appWidgetManager.getAppWidgetIds(provider);
            deviceInfo.put("widgetCount", widgetIds.length);
            
            return deviceInfo.toString();
            
        } catch (Exception e) {
            Log.e(TAG, "获取设备信息失败", e);
            return "{\"error\":\"获取设备信息失败\"}";
        }
    }
    
    /**
     * 获取当前小组件数据
     */
    public String getCurrentWidgetData() {
        SharedPreferences prefs = context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE);
        String data = prefs.getString(KEY_WIDGET_DATA, "{}");
        Log.d(TAG, "当前小组件数据: " + data);
        return data;
    }
    
    /**
     * 清理小组件数据
     */
    public void clearWidgetData() {
        Log.d(TAG, "清理小组件数据");
        
        SharedPreferences prefs = context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE);
        SharedPreferences.Editor editor = prefs.edit();
        editor.clear();
        editor.apply();
        
        // 刷新小组件显示默认数据
        refreshAllWidgets();
        
        Log.i(TAG, "小组件数据已清理");
    }
    
    /**
     * 保存小组件数据到SharedPreferences
     */
    private void saveWidgetData(String jsonData) {
        SharedPreferences prefs = context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE);
        SharedPreferences.Editor editor = prefs.edit();
        editor.putString(KEY_WIDGET_DATA, jsonData);
        editor.apply();
        
        Log.d(TAG, "数据已保存到SharedPreferences");
    }
    
    /**
     * 测试动画播放
     */
    public void testAnimation(String animationType) {
        Log.d(TAG, "测试动画: " + animationType);
        playPetAnimation("Pet_CatBrown", animationType);
    }
    
    /**
     * 获取支持的宠物类型列表
     */
    public String getSupportedPetTypes() {
        try {
            JSONObject result = new JSONObject();
            result.put("petTypes", new JSONArray(PetImageHelper.getSupportedPetTypes()));
            result.put("animationTypes", new JSONArray(PetImageHelper.getSupportedAnimationTypes()));
            return result.toString();
        } catch (Exception e) {
            Log.e(TAG, "获取支持的宠物类型失败", e);
            return "{\"error\":\"获取失败\"}";
        }
    }
}
