package com.zher.meow.widget;

import android.content.Context;
import android.util.Log;

/**
 * 宠物图片资源助手类
 * 处理宠物图片和动画帧的资源映射
 */
public class PetImageHelper {
    
    private static final String TAG = "PetImageHelper";
    
    /**
     * 获取宠物的默认图片资源ID
     */
    public static int getDefaultImageResource(Context context, String prefabName) {
        String petType = extractPetType(prefabName);
        String resourceName = petType.toLowerCase() + "_sit_1";
        
        int resourceId = getDrawableResourceId(context, resourceName);
        if (resourceId == 0) {
            // 如果找不到，使用默认图片
            resourceId = getDrawableResourceId(context, "pet_catbrown_sit_1");
        }
        
        Log.d(TAG, "默认图片: " + prefabName + " -> " + resourceName + " (ID: " + resourceId + ")");
        return resourceId;
    }
    
    /**
     * 获取动画帧资源ID数组
     */
    public static int[] getAnimationFrames(Context context, String prefabName, String animationType) {
        String petType = extractPetType(prefabName);
        
        int frameCount = getAnimationFrameCount(animationType);
        int[] frames = new int[frameCount];
        
        for (int i = 1; i <= frameCount; i++) {
            String resourceName = petType.toLowerCase() + "_" + animationType + "_" + i;
            int resourceId = getDrawableResourceId(context, resourceName);
            
            if (resourceId == 0) {
                // 如果找不到资源，使用默认图片
                resourceId = getDrawableResourceId(context, "pet_catbrown_sit_1");
            }
            
            frames[i - 1] = resourceId;
        }
        
        Log.d(TAG, "动画帧: " + prefabName + " " + animationType + " (" + frameCount + " frames)");
        return frames;
    }
    
    /**
     * 从prefabName提取宠物类型
     */
    private static String extractPetType(String prefabName) {
        if (prefabName.contains("CatBlack")) return "pet_catblack";
        if (prefabName.contains("CatBrown")) return "pet_catbrown";
        if (prefabName.contains("CatGrey")) return "pet_catgrey";
        if (prefabName.contains("CatWhite")) return "pet_catwhite";
        return "pet_catbrown"; // 默认
    }
    
    /**
     * 获取动画帧数
     */
    private static int getAnimationFrameCount(String animationType) {
        switch (animationType.toLowerCase()) {
            case "sit":
                return 5;
            case "run":
                return 4;
            case "laydown":
                return 2;
            default:
                return 1;
        }
    }
    
    /**
     * 通过资源名称获取drawable资源ID
     */
    private static int getDrawableResourceId(Context context, String resourceName) {
        try {
            int resourceId = context.getResources().getIdentifier(
                resourceName, "drawable", context.getPackageName()
            );
            
            if (resourceId == 0) {
                Log.w(TAG, "找不到资源: " + resourceName);
            }
            
            return resourceId;
        } catch (Exception e) {
            Log.e(TAG, "获取资源ID失败: " + resourceName, e);
            return 0;
        }
    }
    
    /**
     * 检查资源是否存在
     */
    public static boolean isResourceExists(Context context, String resourceName) {
        return getDrawableResourceId(context, resourceName) != 0;
    }
    
    /**
     * 获取所有支持的宠物类型
     */
    public static String[] getSupportedPetTypes() {
        return new String[]{
            "Pet_CatBlack",
            "Pet_CatBrown", 
            "Pet_CatGrey",
            "Pet_CatWhite"
        };
    }
    
    /**
     * 获取所有支持的动画类型
     */
    public static String[] getSupportedAnimationTypes() {
        return new String[]{
            "sit",
            "run", 
            "laydown"
        };
    }
    
    /**
     * 验证资源完整性
     */
    public static void validateResources(Context context) {
        Log.d(TAG, "开始验证资源完整性...");
        
        String[] petTypes = getSupportedPetTypes();
        String[] animationTypes = getSupportedAnimationTypes();
        
        int totalResources = 0;
        int missingResources = 0;
        
        for (String petType : petTypes) {
            for (String animationType : animationTypes) {
                int frameCount = getAnimationFrameCount(animationType);
                
                for (int i = 1; i <= frameCount; i++) {
                    String resourceName = extractPetType(petType) + "_" + animationType + "_" + i;
                    totalResources++;
                    
                    if (!isResourceExists(context, resourceName)) {
                        Log.w(TAG, "缺少资源: " + resourceName);
                        missingResources++;
                    }
                }
            }
        }
        
        Log.i(TAG, String.format("资源验证完成: 总计 %d 个资源，缺少 %d 个", 
            totalResources, missingResources));
            
        if (missingResources == 0) {
            Log.i(TAG, "所有资源完整！");
        } else {
            Log.w(TAG, "有资源缺失，可能影响动画播放");
        }
    }
}
