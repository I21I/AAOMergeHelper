# AAO Merge Helper  

## インストール
### 前提条件：Avatar Optimizer
このツールを使用するには、まず <a href="https://vpm.anatawa12.com/avatar-optimizer/ja/#installation" target="_blank">Avatar Optimizer</a> をインストールしてください。

### AAO Merge Helper のインストール

 [インストールページ](https://i21i.github.io/AAOMergeHelper/)からVCCやALCOMに簡単に追加できます。

## お問い合わせ
不具合やお問い合わせについては以下までお願いします：
- <a href="https://x.com/pnpnrkgk" target="_blank">𝕏 / Twitter</a>
- <a href="https://l21l.booth.pm/" target="_blank">Booth</a>

## ✂概要
AAO Merge Helperは、**Avatar Optimizer**の`MergeSkinnedMesh`・`MergePhysBone`を簡単に設定できるツールです。  
MergeSkinnedMeshやMergePhysBoneの設定を自動で行います。

## ✂使い方  
1. Unityに、このUnitypackageをインポート(もしくはVPMリポジトリを追加)  
2. ツールバーから、[Tools]⇒[21CSX]⇒[AAO Merge Helper]を選択  
3. アバターまたは衣装を選択  
4. 除外設定を選び検索  
5. **MergeSkinnedMesh**タブ：SkinnedMeshRenderer(Mesh Renderer)を選択し、下部のマージボタンをクリック
   **MergePhysBone**タブ：PhysBoneを選択し、下部のマージボタンをクリック

> **ヒント**  
> 除外設定は基本そのままで大丈夫です。  
> SkinnedMeshRenderについて、アバター初期はFXで制御されてる場合が多いので、軽量化のためには一番上の除外設定のチェックを外すと良いです。  
> ただし仕様上、オブジェクトのトグルは制御できなくなります。

## ✂環境
- Unity 2022.3.22f1
- VRCSDK 3.8.1-beta.1
- Avatar Optimizer 1.8.10

本ツールの使用に際しては、添付の利用規約（LICENSE.txt）を必ずご確認ください。
