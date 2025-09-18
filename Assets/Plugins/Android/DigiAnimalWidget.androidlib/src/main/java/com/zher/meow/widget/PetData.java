package com.zher.meow.widget;

import org.json.JSONException;
import org.json.JSONObject;

/**
 * 宠物数据模型
 * 对应Unity端的AndroidPetData
 */
public class PetData {
    public String petId;
    public String petName;
    public String prefabName;
    public int energy;
    public int satiety;
    public boolean isBored;
    public String purchaseDate;
    public int ageInDays;
    public String introduction;
    public String lastUpdateTime;
    
    public PetData() {
        this.petId = "";
        this.petName = "我的宠物";
        this.prefabName = "Pet_CatBrown";
        this.energy = 80;
        this.satiety = 70;
        this.isBored = false;
        this.purchaseDate = "";
        this.ageInDays = 1;
        this.introduction = "可爱的宠物";
        this.lastUpdateTime = "";
    }
    
    /**
     * 从JSON对象创建PetData对象
     */
    public static PetData fromJson(JSONObject jsonObject) throws JSONException {
        PetData petData = new PetData();
        
        petData.petId = jsonObject.optString("petId", "");
        petData.petName = jsonObject.optString("petName", "我的宠物");
        petData.prefabName = jsonObject.optString("prefabName", "Pet_CatBrown");
        petData.energy = jsonObject.optInt("energy", 80);
        petData.satiety = jsonObject.optInt("satiety", 70);
        petData.isBored = jsonObject.optBoolean("isBored", false);
        petData.purchaseDate = jsonObject.optString("purchaseDate", "");
        petData.ageInDays = jsonObject.optInt("ageInDays", 1);
        petData.introduction = jsonObject.optString("introduction", "可爱的宠物");
        petData.lastUpdateTime = jsonObject.optString("lastUpdateTime", "");
        
        return petData;
    }
    
    /**
     * 转换为JSON对象
     */
    public JSONObject toJson() throws JSONException {
        JSONObject jsonObject = new JSONObject();
        
        jsonObject.put("petId", petId);
        jsonObject.put("petName", petName);
        jsonObject.put("prefabName", prefabName);
        jsonObject.put("energy", energy);
        jsonObject.put("satiety", satiety);
        jsonObject.put("isBored", isBored);
        jsonObject.put("purchaseDate", purchaseDate);
        jsonObject.put("ageInDays", ageInDays);
        jsonObject.put("introduction", introduction);
        jsonObject.put("lastUpdateTime", lastUpdateTime);
        
        return jsonObject;
    }
    
    /**
     * 获取宠物类型（从prefabName提取）
     */
    public String getPetType() {
        if (prefabName.contains("CatBlack")) return "Pet_CatBlack";
        if (prefabName.contains("CatBrown")) return "Pet_CatBrown";
        if (prefabName.contains("CatGrey")) return "Pet_CatGrey";
        if (prefabName.contains("CatWhite")) return "Pet_CatWhite";
        return "Pet_CatBrown"; // 默认
    }
    
    @Override
    public String toString() {
        return "PetData{" +
                "petId='" + petId + '\'' +
                ", petName='" + petName + '\'' +
                ", prefabName='" + prefabName + '\'' +
                ", energy=" + energy +
                ", satiety=" + satiety +
                ", isBored=" + isBored +
                ", ageInDays=" + ageInDays +
                '}';
    }
}
