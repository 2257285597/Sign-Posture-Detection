from cvzone.HandTrackingModule import HandDetector
import cv2
import socket

cap = cv2.VideoCapture(0)
cap.set(3, 640)
cap.set(4, 360)
# Python端增加：
cap.set(cv2.CAP_PROP_FPS, 60)      # 提升摄像头帧率
cap.set(cv2.CAP_PROP_BUFFERSIZE, 1) # 减少缓冲延迟

success, img = cap.read()
h, w, _ = img.shape
detector = HandDetector(detectionCon=0.8, maxHands=2)

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
serverAddressPort = ("127.0.0.1", 5052)

# 修改后的循环部分（支持双手+左右识别）
while True:
    success, img = cap.read()
    hands, img = detector.findHands(img)
    data = []

    if hands:
        for hand in hands:  # 遍历所有检测到的手
            hand_type = hand["type"]  # 获取左右手信息
            lmList = hand["lmList"]
            # 添加左右手标识（例如 0=左，1=右）
            data.append(0 if hand_type == "Left" else 1)
            for lm in lmList:
                data.extend([lm[0], h - lm[1], lm[2]])

    if len(data) != 0:
        sock.sendto(str.encode(str(data)), serverAddressPort)
    print(len(data))
    print(data)

    cv2.imshow("Image", img)
    cv2.waitKey(1)
