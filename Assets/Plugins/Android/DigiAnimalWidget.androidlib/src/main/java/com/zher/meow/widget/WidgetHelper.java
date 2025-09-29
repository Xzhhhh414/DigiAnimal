package com.zher.meow.widget;

import android.appwidget.AppWidgetManager;
import android.app.PendingIntent;
import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;
import android.os.Bundle;
import android.util.Log;

/**
 * 小组件帮助类
 * 提供打开系统小组件选择界面的功能
 */
public class WidgetHelper {
    
    private static final String TAG = "WidgetHelper";
    
    /**
     * 请求固定小组件到桌面（官方推荐方法）
     * 使用 AppWidgetManager.requestPinAppWidget() API
     * 
     * @param context 上下文
     * @return 返回状态码：0=成功请求, 1=已存在小组件, -1=失败
     */
    public static int openWidgetPicker(Context context) {
        if (context == null) {
            Log.e(TAG, "Context is null");
            return -1;
        }
        
        try {
            // Log.i(TAG, "开始尝试请求固定小组件到桌面");
            // Log.i(TAG, "设备信息: " + android.os.Build.MANUFACTURER + " " + android.os.Build.MODEL);
            
            // 首先检查是否已有小组件
            int widgetCount = getInstalledWidgetCount(context);
            // Log.i(TAG, "当前已安装的小组件数量: " + widgetCount);
            
            if (widgetCount > 0) {
                // Log.i(TAG, "桌面已存在小组件，返回已存在状态");
                return 1; // 已存在小组件
            }
            
            // 使用官方推荐的 requestPinAppWidget 方法
            if (requestPinWidget(context)) {
                // Log.i(TAG, "成功请求固定小组件");
                return 0; // 成功发起请求
            }
            
            // 降级方案：尝试其他方法
            Log.w(TAG, "requestPinAppWidget 不支持，尝试降级方案");
            if (tryFallbackMethods(context)) {
                return 0; // 降级方案成功
            }
            
            return -1; // 所有方案都失败
            
        } catch (Exception e) {
            Log.e(TAG, "请求固定小组件时发生错误: " + e.getMessage());
            e.printStackTrace();
            return -1;
        }
    }
    
    /**
     * 使用官方API请求固定小组件
     */
    private static boolean requestPinWidget(Context context) {
        try {
            AppWidgetManager appWidgetManager = AppWidgetManager.getInstance(context);
            
            // 检查是否支持固定小组件
            if (!appWidgetManager.isRequestPinAppWidgetSupported()) {
                Log.w(TAG, "当前启动器不支持 requestPinAppWidget");
                return false;
            }
            
        // Log.i(TAG, "启动器支持 requestPinAppWidget，开始请求");
        
        // 创建小组件提供者组件名
        ComponentName provider = new ComponentName(context, DigiAnimalWidgetProvider.class);
        // Log.d(TAG, "小组件提供者: " + provider.toString());
            
            // 可选：为小组件设置初始配置
            Bundle extras = new Bundle();
            extras.putString("source", "game_button");
            extras.putLong("timestamp", System.currentTimeMillis());
            
            // 可选：设置成功回调（当用户确认添加小组件时触发）
            Intent callbackIntent = new Intent(context, DigiAnimalWidgetProvider.class);
            callbackIntent.setAction("com.zher.meow.widget.WIDGET_PINNED");
            
            PendingIntent successCallback = PendingIntent.getBroadcast(
                context, 
                0, 
                callbackIntent,
                PendingIntent.FLAG_UPDATE_CURRENT | PendingIntent.FLAG_IMMUTABLE
            );
            
            // 发起请求固定小组件
            boolean result = appWidgetManager.requestPinAppWidget(provider, extras, successCallback);
            
        // if (result) {
        //     Log.i(TAG, "requestPinAppWidget 调用成功，等待用户确认");
        // } else {
        //     Log.w(TAG, "requestPinAppWidget 调用返回 false");
        // }
            
            return result;
            
        } catch (Exception e) {
            Log.e(TAG, "requestPinWidget 失败: " + e.getMessage());
            e.printStackTrace();
            return false;
        }
    }
    
    /**
     * 降级方案：尝试其他方法
     */
    private static boolean tryFallbackMethods(Context context) {
        Log.i(TAG, "开始尝试降级方案");
        
        // 方案1：尝试传统的小组件选择器
        if (tryOpenWithPreSelection(context)) {
            Log.i(TAG, "降级方案1成功：小组件选择器");
            return true;
        }
        
        // 方案2：回到桌面让用户手动添加
        if (tryOpenLauncher(context)) {
            Log.i(TAG, "降级方案2成功：回到桌面");
            return true;
        }
        
        Log.w(TAG, "所有降级方案都失败了");
        return false;
    }
    
    /**
     * 添加一个专门用于Unity回调的方法
     * 当小组件成功添加时，Unity会调用这个方法
     */
    public static void onWidgetAddedSuccess(Context context) {
        Log.i(TAG, "收到小组件添加成功通知");
        // 这里可以做一些额外的处理，比如统计等
        
        // 通过Unity的AndroidJavaClass回调到C#
        try {
            // 使用Unity的消息系统发送回调
            // 这里我们使用Unity的UnitySendMessage方法
            // 但由于这是静态方法，我们需要在C#端设置一个GameObject来接收消息
        } catch (Exception e) {
            Log.e(TAG, "回调Unity失败: " + e.getMessage());
        }
    }
    
    /**
     * 尝试打开小组件选择界面并预选我们的小组件
     */
    private static boolean tryOpenWithPreSelection(Context context) {
        try {
            Intent intent = new Intent(AppWidgetManager.ACTION_APPWIDGET_PICK);
            
            // 尝试添加我们的小组件提供者信息（可能在某些设备上有效）
            ComponentName provider = new ComponentName(context, DigiAnimalWidgetProvider.class);
            intent.putExtra(AppWidgetManager.EXTRA_APPWIDGET_PROVIDER, provider);
            
            // 添加一些可能有用的额外信息
            intent.putExtra("android.intent.extra.shortcut.NAME", "Miao屋虚拟宠物");
            
            Log.d(TAG, "尝试预选模式，Intent: " + intent.toString());
            Log.d(TAG, "Provider: " + provider.toString());
            
            // 确保Intent可以被处理
            if (intent.resolveActivity(context.getPackageManager()) != null) {
                intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
                Log.d(TAG, "Intent可以被处理，正在启动Activity");
                context.startActivity(intent);
                return true;
            } else {
                Log.w(TAG, "Intent无法被处理，没有找到对应的Activity");
            }
            
            return false;
            
        } catch (Exception e) {
            Log.w(TAG, "预选模式失败: " + e.getMessage());
            e.printStackTrace();
            return false;
        }
    }
    
    /**
     * 打开普通的小组件选择界面
     */
    private static boolean tryOpenNormalPicker(Context context) {
        try {
            // 尝试多种不同的Intent
            String[] intentActions = {
                AppWidgetManager.ACTION_APPWIDGET_PICK,
                "android.appwidget.action.APPWIDGET_PICK",
                "com.android.launcher.action.INSTALL_SHORTCUT"
            };
            
            for (String action : intentActions) {
                try {
                    Intent intent = new Intent(action);
                    Log.d(TAG, "尝试Intent action: " + action);
                    
                    // 确保Intent可以被处理
                    if (intent.resolveActivity(context.getPackageManager()) != null) {
                        intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
                        Log.d(TAG, "找到可处理的Activity，正在启动: " + action);
                        context.startActivity(intent);
                        return true;
                    } else {
                        Log.d(TAG, "无法处理Intent action: " + action);
                    }
                } catch (Exception e) {
                    Log.d(TAG, "Intent action失败: " + action + ", 错误: " + e.getMessage());
                }
            }
            
            return false;
            
        } catch (Exception e) {
            Log.w(TAG, "普通模式失败: " + e.getMessage());
            return false;
        }
    }
    
    /**
     * 尝试特定厂商的小组件选择界面
     */
    private static boolean tryManufacturerSpecificWidgetPicker(Context context) {
        try {
            // 获取设备厂商信息
            String manufacturer = android.os.Build.MANUFACTURER.toLowerCase();
            String model = android.os.Build.MODEL.toLowerCase();
            Log.d(TAG, "设备厂商: " + manufacturer + ", 型号: " + model);
            
            // 尝试不同厂商的特定Intent
            String[] manufacturerIntents = {
                // MIUI (小米)
                "com.miui.home.launcher.action.ADD_WIDGET",
                "com.miui.home.ADD_WIDGET",
                // EMUI (华为)
                "com.huawei.android.launcher.action.ADD_WIDGET",
                // ColorOS (OPPO)
                "com.oppo.launcher.action.ADD_WIDGET",
                // OneUI (三星)
                "com.samsung.android.launcher.action.ADD_WIDGET",
                // 通用尝试
                "android.intent.action.CREATE_SHORTCUT",
                "com.android.launcher.action.INSTALL_WIDGET"
            };
            
            for (String action : manufacturerIntents) {
                try {
                    Intent intent = new Intent(action);
                    Log.d(TAG, "尝试厂商特定Intent: " + action);
                    
                    if (intent.resolveActivity(context.getPackageManager()) != null) {
                        intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
                        Log.d(TAG, "找到厂商特定Activity，正在启动: " + action);
                        context.startActivity(intent);
                        return true;
                    } else {
                        Log.d(TAG, "无法处理厂商Intent: " + action);
                    }
                } catch (Exception e) {
                    Log.d(TAG, "厂商Intent失败: " + action + ", 错误: " + e.getMessage());
                }
            }
            
            return false;
            
        } catch (Exception e) {
            Log.w(TAG, "厂商特定方法失败: " + e.getMessage());
            return false;
        }
    }
    
    /**
     * 尝试打开桌面启动器作为降级方案
     */
    private static boolean tryOpenLauncher(Context context) {
        try {
            // 回到桌面，用户可以手动长按添加小组件
            Intent intent = new Intent(Intent.ACTION_MAIN);
            intent.addCategory(Intent.CATEGORY_HOME);
            intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
            
            if (intent.resolveActivity(context.getPackageManager()) != null) {
                context.startActivity(intent);
                return true;
            }
            
            return false;
            
        } catch (Exception e) {
            Log.w(TAG, "打开桌面失败: " + e.getMessage());
            return false;
        }
    }
    
    /**
     * 检查系统是否支持小组件
     */
    public static boolean isWidgetSupported(Context context) {
        if (context == null) {
            return false;
        }
        
        try {
            Intent intent = new Intent(AppWidgetManager.ACTION_APPWIDGET_PICK);
            return intent.resolveActivity(context.getPackageManager()) != null;
        } catch (Exception e) {
            Log.e(TAG, "检查小组件支持时发生错误: " + e.getMessage());
            return false;
        }
    }
    
    /**
     * 获取当前已添加的小组件数量
     */
    public static int getInstalledWidgetCount(Context context) {
        if (context == null) {
            Log.w(TAG, "getInstalledWidgetCount: context为null");
            return 0;
        }
        
        try {
            AppWidgetManager appWidgetManager = AppWidgetManager.getInstance(context);
            ComponentName provider = new ComponentName(context, DigiAnimalWidgetProvider.class);
            int[] widgetIds = appWidgetManager.getAppWidgetIds(provider);
            
            if (widgetIds != null) {
            // Log.d(TAG, "找到小组件IDs: " + java.util.Arrays.toString(widgetIds));
            return widgetIds.length;
        } else {
            // Log.d(TAG, "widgetIds为null");
            return 0;
        }
        } catch (Exception e) {
            Log.e(TAG, "获取小组件数量时发生错误: " + e.getMessage());
            e.printStackTrace();
            return 0;
        }
    }
}

