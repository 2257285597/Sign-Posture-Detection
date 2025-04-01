# -*- coding: utf-8 -*-
import cv2
import mediapipe as mp
import numpy as np
import tensorflow as tf
import socket
import json
import os
import time
import sys
import io

# 强制使用UTF-8编码输出
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')


class DeepLearningGestureRecognizer:
    def __init__(self, model_path="gesture_model.h5", ip="127.0.0.1", port=5065):
        # 初始化MediaPipe Hands用于手部关键点检测
        self.mp_hands = mp.solutions.hands
        self.hands = self.mp_hands.Hands(
            static_image_mode=False,
            max_num_hands=2,
            min_detection_confidence=0.7,
            min_tracking_confidence=0.5
        )
        self.mp_drawing = mp.solutions.drawing_utils
        self.mp_drawing_styles = mp.solutions.drawing_styles

        # 如果存在则加载深度学习模型
        if os.path.exists(model_path):
            try:
                self.model = tf.keras.models.load_model(model_path)
                self.has_model = True
                print(f"模型加载成功: {model_path}")
            except Exception as e:
                self.has_model = False
                print(f"加载模型错误: {e}")
                print("使用几何方法进行手势识别")
        else:
            self.has_model = False
            print(f"未找到模型文件: {model_path}，使用几何方法")

        # 手势标签（保持英文以避免编码问题）
        self.gesture_labels = [
            "None", "OpenPalm", "Fist", "PointingIndex", "OkSign", "Victory",
            "ThumbsUp", "ThumbsDown"
        ]

        # 初始化UDP Socket用于与Unity通信
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.unity_ip = ip
        self.unity_port = port

        # 显示设置
        self.show_landmarks = True
        self.show_fps = True
        self.frame_count = 0
        self.start_time = time.time()
        self.fps = 0

    def create_training_model(self):
        """创建手势识别的深度学习模型"""
        model = tf.keras.Sequential([
            tf.keras.layers.Input(shape=(21, 3)),  # 21个关键点，每个3个坐标
            tf.keras.layers.Flatten(),
            tf.keras.layers.Dense(128, activation='relu'),
            tf.keras.layers.Dropout(0.2),
            tf.keras.layers.Dense(64, activation='relu'),
            tf.keras.layers.Dropout(0.2),
            tf.keras.layers.Dense(len(self.gesture_labels), activation='softmax')
        ])

        model.compile(
            optimizer='adam',
            loss='sparse_categorical_crossentropy',
            metrics=['accuracy']
        )

        return model

    def collect_training_data(self, output_folder="gesture_data", gestures_to_collect=None):
        """收集手势训练数据"""
        if gestures_to_collect is None:
            gestures_to_collect = self.gesture_labels[1:6]  # 默认：前5种手势类型

        os.makedirs(output_folder, exist_ok=True)

        cap = cv2.VideoCapture(0)

        for gesture_idx, gesture_name in enumerate(gestures_to_collect):
            gesture_data = []
            samples_count = 0
            max_samples = 100  # 每种手势100个样本

            print(f"准备收集手势: {gesture_name}, 按空格键开始...")
            wait_key = True

            while samples_count < max_samples:
                success, frame = cap.read()
                if not success:
                    continue

                # 处理帧以检测手部
                rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
                results = self.hands.process(rgb_frame)

                # 如果检测到手部，绘制关键点
                if results.multi_hand_landmarks:
                    for hand_landmarks in results.multi_hand_landmarks:
                        self.mp_drawing.draw_landmarks(
                            frame,
                            hand_landmarks,
                            self.mp_hands.HAND_CONNECTIONS,
                            self.mp_drawing_styles.get_default_hand_landmarks_style(),
                            self.mp_drawing_styles.get_default_hand_connections_style()
                        )

                # 显示指导信息
                cv2.putText(
                    frame,
                    f"Collecting: {gesture_name} ({samples_count}/{max_samples})",
                    (10, 30),
                    cv2.FONT_HERSHEY_SIMPLEX,
                    0.8,
                    (0, 255, 0),
                    2
                )

                if wait_key:
                    cv2.putText(
                        frame,
                        "Press SPACE to start collecting",
                        (10, 60),
                        cv2.FONT_HERSHEY_SIMPLEX,
                        0.8,
                        (0, 255, 0),
                        2
                    )
                else:
                    cv2.putText(
                        frame,
                        "Collecting data... Keep the pose steady",
                        (10, 60),
                        cv2.FONT_HERSHEY_SIMPLEX,
                        0.8,
                        (0, 0, 255),
                        2
                    )

                # 在收集过程中显示关键点信息
                if results.multi_hand_landmarks and not wait_key:
                    hand_landmarks = results.multi_hand_landmarks[0]  # 使用第一只手
                    for i, landmark in enumerate(hand_landmarks.landmark):
                        x, y = int(landmark.x * frame.shape[1]), int(landmark.y * frame.shape[0])
                        # 显示关键点索引和坐标
                        cv2.putText(
                            frame,
                            f"{i}: ({landmark.x:.2f}, {landmark.y:.2f}, {landmark.z:.2f})",
                            (10, 90 + i * 20),
                            cv2.FONT_HERSHEY_SIMPLEX,
                            0.5,
                            (255, 0, 0),
                            1
                        )

                cv2.imshow("Gesture Data Collection", frame)
                key = cv2.waitKey(5)

                if key & 0xFF == 27:  # ESC键退出
                    cap.release()
                    cv2.destroyAllWindows()
                    return

                if wait_key and key == 32:  # 空格键
                    wait_key = False
                    continue

                if not wait_key:
                    # 处理帧以提取手部关键点
                    if results.multi_hand_landmarks:
                        for hand_landmarks in results.multi_hand_landmarks:
                            # 收集关键点数据
                            landmarks_data = []
                            for landmark in hand_landmarks.landmark:
                                landmarks_data.append([landmark.x, landmark.y, landmark.z])

                            # 添加到数据集
                            gesture_data.append({
                                "landmarks": landmarks_data,
                                "label": gesture_idx
                            })

                            samples_count += 1
                            if samples_count >= max_samples:
                                break

                    # 短暂延迟以获取不同姿势
                    time.sleep(0.1)

            # 保存当前手势数据
            output_file = os.path.join(output_folder, f"{gesture_name}.json")
            with open(output_file, 'w') as f:
                json.dump(gesture_data, f)

            print(f"已保存 {samples_count} 个 {gesture_name} 样本到 {output_file}")

        cap.release()
        cv2.destroyAllWindows()

    def train_model(self, data_folder="gesture_data", output_model="gesture_model.h5", epochs=50):
        """训练手势识别模型"""
        X = []
        y = []

        # 加载训练数据
        for gesture_idx, gesture_name in enumerate(self.gesture_labels[1:]):  # 跳过"None"
            data_file = os.path.join(data_folder, f"{gesture_name}.json")
            if os.path.exists(data_file):
                with open(data_file, 'r') as f:
                    gesture_data = json.load(f)

                    for sample in gesture_data:
                        X.append(sample["landmarks"])
                        y.append(sample["label"])

        if len(X) == 0:
            print("未找到训练数据。请先收集数据。")
            return

        X = np.array(X)
        y = np.array(y)

        print(f"使用 {len(X)} 个样本训练，包含 {len(set(y))} 种手势类型")

        # 创建模型
        model = self.create_training_model()

        # 训练模型
        model.fit(X, y, epochs=epochs, validation_split=0.2)

        # 保存模型
        model.save(output_model)
        print(f"模型已保存到 {output_model}")

        self.model = model
        self.has_model = True

    def preprocess_landmarks(self, landmarks):
        """预处理关键点数据用于模型输入"""
        points = []
        for landmark in landmarks.landmark:
            points.append([landmark.x, landmark.y, landmark.z])
        return np.array([points])

    def recognize_gesture_with_model(self, landmarks):
        """使用深度学习模型识别手势"""
        processed_data = self.preprocess_landmarks(landmarks)
        predictions = self.model.predict(processed_data, verbose=0)
        gesture_idx = np.argmax(predictions[0])
        confidence = predictions[0][gesture_idx]

        # 如果置信度太低，返回None
        if confidence < 0.7:
            return 0, confidence

        return gesture_idx, confidence

    def recognize_gesture_geometric(self, landmarks):
        """使用几何方法识别手势"""
        # 提取关键点
        points = np.array([[lm.x, lm.y, lm.z] for lm in landmarks.landmark])

        # 计算手掌中心和大小
        palm_center = np.mean(points[[0, 5, 9, 13, 17]], axis=0)
        palm_size = np.linalg.norm(points[5] - points[17])

        # 指尖索引
        fingertips = [4, 8, 12, 16, 20]  # 拇指，食指，中指，无名指，小指

        # 计算指尖到手掌中心的距离
        tip_distances = [np.linalg.norm(points[tip] - palm_center) for tip in fingertips]

        # 根据距离阈值检查手指是否伸展
        threshold = palm_size * 0.5
        is_extended = [dist > threshold for dist in tip_distances]

        # 拇指特殊检查（拇指比其他手指短）
        thumb_threshold = palm_size * 0.35
        is_extended[0] = tip_distances[0] > thumb_threshold

        # 拇指和食指间距，用于OK手势
        thumb_index_distance = np.linalg.norm(points[4] - points[8])

        # 手势识别逻辑
        # 1. 张开手掌 - 所有手指伸展
        if all(is_extended):
            return 1  # OpenPalm

        # 2. 握拳 - 没有手指伸展
        if not any(is_extended):
            return 2  # Fist

        # 3. 食指指点 - 仅食指伸展
        if is_extended[1] and not any(is_extended[i] for i in [0, 2, 3, 4]):
            return 3  # PointingIndex

        # 4. OK手势 - 拇指和食指形成圆圈
        if thumb_index_distance < palm_size * 0.1:
            return 4  # OkSign

        # 5. 胜利手势 - 食指和中指伸展
        if is_extended[1] and is_extended[2] and not any(is_extended[i] for i in [0, 3, 4]):
            return 5  # Victory

        # 6. 大拇指向上 - 仅拇指伸展，向上指
        if is_extended[0] and not any(is_extended[1:]) and points[4][1] < points[3][1]:
            return 6  # ThumbsUp

        # 7. 大拇指向下 - 仅拇指伸展，向下指
        if is_extended[0] and not any(is_extended[1:]) and points[4][1] > points[3][1]:
            return 7  # ThumbsDown

        return 0  # None/未知

    def process_frame(self, frame):
        """处理视频帧并识别手势"""
        # 转换为RGB格式
        rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)

        # 处理手部关键点
        results = self.hands.process(rgb_frame)

        # 跟踪FPS
        self.frame_count += 1
        elapsed_time = time.time() - self.start_time
        if elapsed_time > 1.0:  # 每秒更新一次FPS
            self.fps = self.frame_count / elapsed_time
            self.frame_count = 0
            self.start_time = time.time()

        # 显示FPS
        if self.show_fps:
            cv2.putText(
                frame,
                f"FPS: {self.fps:.1f}",
                (frame.shape[1] - 120, 30),
                cv2.FONT_HERSHEY_SIMPLEX,
                0.7,
                (0, 255, 0),
                2
            )

        # 处理结果
        gesture_data = []

        if results.multi_hand_landmarks:
            for hand_idx, hand_landmarks in enumerate(results.multi_hand_landmarks):
                # 绘制手部关键点
                if self.show_landmarks:
                    self.mp_drawing.draw_landmarks(
                        frame,
                        hand_landmarks,
                        self.mp_hands.HAND_CONNECTIONS,
                        self.mp_drawing_styles.get_default_hand_landmarks_style(),
                        self.mp_drawing_styles.get_default_hand_connections_style()
                    )

                # 确定手的类型（左/右）
                handedness = 0  # 默认：左手
                if results.multi_handedness and len(results.multi_handedness) > hand_idx:
                    handedness_info = results.multi_handedness[hand_idx]
                    if handedness_info.classification[0].label == "Right":
                        handedness = 1  # 右手

                # 识别手势
                if self.has_model:
                    gesture, confidence = self.recognize_gesture_with_model(hand_landmarks)
                    # 显示置信度
                    cv2.putText(
                        frame,
                        f"Confidence: {confidence:.2f}",
                        (10, 90 + hand_idx * 30),
                        cv2.FONT_HERSHEY_SIMPLEX,
                        0.6,
                        (255, 255, 0),
                        1
                    )
                else:
                    gesture = self.recognize_gesture_geometric(hand_landmarks)
                    confidence = 1.0

                # 收集关键点数据
                landmarks_data = []
                for landmark in hand_landmarks.landmark:
                    landmarks_data.extend([landmark.x, landmark.y, landmark.z])

                # 添加手的类型和手势类型到数据中
                hand_data = {
                    "hand_type": handedness,
                    "gesture_type": int(gesture),
                    "landmarks": landmarks_data
                }
                gesture_data.append(hand_data)

                # 显示手势信息
                gesture_name = self.gesture_labels[gesture]
                hand_type_text = "Right Hand" if handedness == 0 else "Left Hand"
                cv2.putText(
                    frame,
                    f"{hand_type_text}: {gesture_name}",
                    (10, 30 + hand_idx * 30),
                    cv2.FONT_HERSHEY_SIMPLEX,
                    0.8,
                    (0, 255, 0),
                    2
                )

        # 发送数据到Unity
        if gesture_data:
            self.send_to_unity(gesture_data)

        return frame

    def send_to_unity(self, gesture_data):
        """将手势数据发送到Unity"""
        try:
            json_data = json.dumps(gesture_data)
            self.sock.sendto(json_data.encode(), (self.unity_ip, self.unity_port))
        except Exception as e:
            print(f"发送数据错误: {e}")

    def start_camera(self, camera_id=0):
        """启动摄像头和处理循环"""
        cap = cv2.VideoCapture(camera_id)

        if not cap.isOpened():
            print("错误：无法打开摄像头。")
            return

        # 设置摄像头分辨率
        cap.set(cv2.CAP_PROP_FRAME_WIDTH, 640)
        cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 480)

        print(f"正在发送手势数据到Unity: {self.unity_ip}:{self.unity_port}")
        print("按'ESC'退出, 'C'收集训练数据, 'T'训练模型")

        while cap.isOpened():
            success, frame = cap.read()
            if not success:
                print("错误：无法读取帧。")
                break

            # 处理帧
            processed_frame = self.process_frame(frame)

            # 显示处理后的帧
            cv2.imshow("Deep Learning Hand Gesture Recognition", processed_frame)

            # 显示帮助信息
            help_frame = np.zeros((200, 300, 3), dtype=np.uint8)
            cv2.putText(help_frame, "Controls:", (10, 30), cv2.FONT_HERSHEY_SIMPLEX, 0.7, (255, 255, 255), 1)
            cv2.putText(help_frame, "ESC - Exit", (10, 60), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255, 255, 255), 1)
            cv2.putText(help_frame, "C - Collect training data", (10, 90), cv2.FONT_HERSHEY_SIMPLEX, 0.6,
                        (255, 255, 255), 1)
            cv2.putText(help_frame, "T - Train model", (10, 120), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255, 255, 255), 1)
            cv2.putText(help_frame, "L - Toggle landmarks", (10, 150), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255, 255, 255),
                        1)
            cv2.putText(help_frame, "F - Toggle FPS display", (10, 180), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255, 255, 255),
                        1)
            cv2.imshow("Help", help_frame)

            # 处理按键
            key = cv2.waitKey(5) & 0xFF
            if key == 27:  # ESC键
                break
            elif key == ord('c'):  # 'C'键 - 收集数据
                cap.release()
                cv2.destroyAllWindows()
                self.collect_training_data()
                cap = cv2.VideoCapture(camera_id)
            elif key == ord('t'):  # 'T'键 - 训练模型
                cap.release()
                cv2.destroyAllWindows()
                self.train_model()
                cap = cv2.VideoCapture(camera_id)
            elif key == ord('l'):  # 'L'键 - 切换显示关键点
                self.show_landmarks = not self.show_landmarks
            elif key == ord('f'):  # 'F'键 - 切换显示FPS
                self.show_fps = not self.show_fps

        # 释放资源
        cap.release()
        cv2.destroyAllWindows()


if __name__ == "__main__":
    # 解析命令行参数
    import argparse

    parser = argparse.ArgumentParser(description="深度学习手势识别")
    parser.add_argument("--ip", type=str, default="127.0.0.1", help="Unity接收器IP地址")
    parser.add_argument("--port", type=int, default=5065, help="Unity接收器端口")
    parser.add_argument("--model", type=str, default="gesture_model.h5", help="模型文件路径")
    parser.add_argument("--camera", type=int, default=0, help="摄像头设备ID")

    args = parser.parse_args()

    # 创建手势识别器并启动
    recognizer = DeepLearningGestureRecognizer(
        model_path=args.model,
        ip=args.ip,
        port=args.port
    )

    recognizer.start_camera(camera_id=args.camera)