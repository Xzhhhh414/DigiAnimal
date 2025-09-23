package com.unity3d.player;

import android.app.Activity;
import android.app.AlertDialog;
import android.content.DialogInterface;
import android.content.Intent;
import android.content.SharedPreferences;
import android.os.Bundle;
import android.webkit.WebView;
 
public class PrivacyActivity extends Activity implements DialogInterface.OnClickListener {

    // 隐私协议内容
    final String privacyContext =
            "欢迎使用Miao屋，在使用本游戏前，请您充分阅读并理解 <a href=\"https://xzhhhh.notion.site/Miao-277f7c91b1a080ccbab6e92dcdef63fd\">" +
            "《用户协议》</a>和<a href=\"https://xzhhhh.notion.site/Miao-256f7c91b1a0806bb4e1d343a86667b5\">《隐私政策》</a>各条款;\n\n" +
            "1.保护用户隐私是本游戏的一项基本政策，本游戏不会泄露您的个人信息；\n\n" +
            "2.我们会根据您使用的具体功能需要，收集必要的用户信息（如申请设备信息，存储等相关权限）；\n\n" +
            "3.在您同意App隐私政策后，我们将进行集成SDK的初始化工作，会收集您的android_id、Mac地址、IMEI和应用安装列表，以保障App正常数据统计和安全风控；\n\n" +
            "4.为了方便您的查阅，您可以通过\"设置\"重新查看该协议；\n\n" +
            "5.您可以阅读完整版的隐私保护政策了解我们申请使用相关权限的情况，以及对您个人隐私的保护措施。\n\n" +
            "请点击\"同意\"继续使用本游戏，或点击\"拒绝\"退出应用。";
     
    
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        
        // 检查是否为老用户，如果是则立即跳转（最小化处理时间）
        if (GetPrivacyAccept()){
            // 老用户直接跳转，无任何UI设置和过渡效果
            EnterUnityActivityDirectly();
            return;
        }
        
        // 新用户才需要设置透明主题和显示隐私协议
        // 设置窗口标志，消除启动画面
        getWindow().setFlags(
            android.view.WindowManager.LayoutParams.FLAG_FULLSCREEN,
            android.view.WindowManager.LayoutParams.FLAG_FULLSCREEN
        );
        
        // 设置透明背景，避免白屏
        getWindow().setBackgroundDrawableResource(android.R.color.transparent);
        
        // 禁用预览窗口（启动画面）
        getWindow().addFlags(android.view.WindowManager.LayoutParams.FLAG_DRAWS_SYSTEM_BAR_BACKGROUNDS);
        
        // 弹出隐私协议对话框
        ShowPrivacyDialog();
    }
 
    // 显示隐私协议对话框
    private void ShowPrivacyDialog(){
        WebView webView = new WebView(this);
        webView.loadData(privacyContext, "text/html", "utf-8");         
        AlertDialog.Builder privacyDialog = new AlertDialog.Builder(this);
        privacyDialog.setCancelable(false);
        privacyDialog.setView(webView);
        privacyDialog.setTitle("隐私协议");
        privacyDialog.setNegativeButton("拒绝",this);
        privacyDialog.setPositiveButton("同意",this);
        privacyDialog.create().show();
    }
    
    @Override
    public void onClick(DialogInterface dialogInterface, int i) {
        switch (i){
            case AlertDialog.BUTTON_POSITIVE://点击同意按钮
                SetPrivacyAccept(true);
                EnterUnityActivity(); //启动Unity Activity
                break;
            case AlertDialog.BUTTON_NEGATIVE://点击拒绝按钮,直接退出App
                finish();
                break;
        }
    }
    
    // 启动Unity Activity（新用户同意后）
    private void EnterUnityActivity(){
        Intent unityAct = new Intent();
        unityAct.setClassName(this, "com.unity3d.player.UnityPlayerActivity");
        this.startActivity(unityAct);
        // 关闭当前Activity，避免用户按返回键回到隐私协议页面
        finish();
    }
    
    // 直接启动Unity Activity（老用户，无过渡）
    private void EnterUnityActivityDirectly(){
        Intent unityAct = new Intent();
        unityAct.setClassName(this, "com.unity3d.player.UnityPlayerActivity");
        // 添加标志，直接替换当前Activity，无动画
        unityAct.addFlags(Intent.FLAG_ACTIVITY_NO_ANIMATION);
        unityAct.addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP);
        unityAct.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
        this.startActivity(unityAct);
        // 立即关闭当前Activity，无动画
        finish();
        // 禁用Activity切换动画
        overridePendingTransition(0, 0);
    }
    
    // 本地存储保存同意隐私协议状态
    private void SetPrivacyAccept(boolean accepted){
        SharedPreferences.Editor prefs = this.getSharedPreferences("PlayerPrefs", MODE_PRIVATE).edit();
        prefs.putBoolean("PrivacyAcceptedKey", accepted);
        prefs.apply();
    }
    
    // 获取是否已经同意过
    private boolean GetPrivacyAccept(){
        SharedPreferences prefs = this.getSharedPreferences("PlayerPrefs", MODE_PRIVATE);
        return prefs.getBoolean("PrivacyAcceptedKey", false);
    }
}