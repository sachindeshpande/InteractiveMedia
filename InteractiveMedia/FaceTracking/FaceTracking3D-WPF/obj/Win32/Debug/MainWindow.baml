   M S B A M L   `   `   `  ����M  IFaceTracking3D-WPF, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null/clr-namespace:FaceTracking3DFaceTracking3D  5   	       FaceTracking3D.MainWindow   5   	   W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   .localclr-namespace:FaceTracking3D     5   	     � Title$  Face Tracking 3D��5   	   $	��735��5   	   $
��1024��6    � 	Resources 5	         MediumGreyBrush �� Color  KinectPurpleBrush  KinectBlueBrush �� 
TargetType �� Property �� Value��%�   &	        &	 #     &	 F     '�� i     �� 5
   
   $ 	#ff6e6e6e��62   �� 5   
   $ 	#ff52318f��64   �� 5   
   $ 	#ff00BCF2��62   �� 5   
   " ��6   .�5      ��  �@J�SnapsToDevicePixels . 64   5      5      .��5      � -   5        $��
layoutGrid��5      $��
10 0 10 10q�6   ��5   
   �� 5       �� ImageSource$ Images\Kinetech.png��6    �� Opacity$	 0.3Q�6;   .+�5      �� $��Stretch=�6   $��Stretch=�62   $��2q�6N   
���i��._�5      "   FaceTracking3D.FaceGridPanel  -   5          Name$ faceGridPanel��5   *   5      (� 5$   
   	 (� Stretch$	 Uniform=�6   $��Center=�6$   .��5%      � .+�5&      +   %FaceTracking3D.TexturedFaceMeshViewer  -   5        
  Name$
 faceTrackingVisualizer��5&   /   5'      5(      5)      5*      