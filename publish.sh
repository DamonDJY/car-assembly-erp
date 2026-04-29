#!/bin/bash
set -e

# ============================================================
# 发布脚本 - 解决 SSH 22 端口不通的问题，使用 HTTPS + Token 推送
# ============================================================

echo "========================================"
echo "CarAssemblyErp GitHub 发布助手"
echo "========================================"
echo ""
echo "检测到 SSH 22 端口连接 GitHub 失败，"
echo "将使用 HTTPS + Personal Access Token 方式推送。"
echo ""

# 读取 GitHub 用户名
read -p "请输入你的 GitHub 用户名: " GH_USER

# 读取仓库名（默认 car-assembly-erp）
read -p "请输入仓库名 [默认: car-assembly-erp]: " GH_REPO
GH_REPO=${GH_REPO:-car-assembly-erp}

# 读取 Token
read -s -p "请输入你的 GitHub Personal Access Token: " GH_TOKEN
echo ""

# 确认信息
echo ""
echo "即将推送到: https://github.com/${GH_USER}/${GH_REPO}.git"
read -p "确认吗? [y/N]: " CONFIRM

if [[ ! "$CONFIRM" =~ ^[Yy]$ ]]; then
    echo "已取消"
    exit 1
fi

# 配置 remote（使用 token 嵌入 URL 的方式，避免交互式密码输入）
REMOTE_URL="https://${GH_USER}:${GH_TOKEN}@github.com/${GH_USER}/${GH_REPO}.git"

# 如果 remote 已存在则移除
if git remote get-url origin &>/dev/null; then
    echo "移除已有的 origin remote..."
    git remote remove origin
fi

echo "添加 origin remote..."
git remote add origin "$REMOTE_URL"

# 确保分支名为 main
if git branch --show-current | grep -q "main"; then
    echo "分支已是 main"
else
    echo "重命名分支为 main..."
    git branch -M main
fi

# 推送
echo "开始推送到 GitHub..."
if git push -u origin main; then
    echo ""
    echo "========================================"
    echo "✅ 推送成功!"
    echo "========================================"
    echo ""
    echo "仓库地址: https://github.com/${GH_USER}/${GH_REPO}"
    echo ""
    echo "Render 部署步骤:"
    echo "1. 打开 https://dashboard.render.com/"
    echo "2. 点击 New + → Blueprint"
    echo "3. 选择 ${GH_USER}/${GH_REPO} 仓库"
    echo "4. 点击 Connect，Render 会自动读取 render.yaml"
    echo "5. 等待构建完成 (约 5-10 分钟)"
    echo ""
else
    echo ""
    echo "❌ 推送失败"
    echo "请检查:"
    echo "  - Token 是否有 'repo' 权限"
    echo "  - 仓库是否已存在 (若不存在会先创建)"
    echo "  - 用户名和仓库名是否正确"
    exit 1
fi

# 清理 remote URL 中的 token（为了安全）
git remote set-url origin "https://github.com/${GH_USER}/${GH_REPO}.git"
echo "已清理 remote URL 中的 token (安全)"
