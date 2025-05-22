from flask import Flask, request, jsonify
from ultralytics import YOLO
import cv2
import numpy as np
app = Flask(__name__)

model = YOLO('best.pt')
threshold = 0.5


@app.route('/upload_from_unity', methods=['POST'])
def upload_image():
    global threshold

    if 'image' not in request.files:
        return jsonify({"status": "error", "message": "No image part"}), 400

    file = request.files['image']
    if file.filename == '':
        return jsonify({"status": "error", "message": "No selected file"}), 400

    image_bytes = file.read()
    npimg = np.frombuffer(image_bytes, np.uint8)
    frame = cv2.imdecode(npimg, cv2.IMREAD_COLOR)

    if frame is None:
        return jsonify({'error': 'Invalid image'}), 400

    model_results = model(frame)
    final_results = {}

    for box in model_results[0].boxes:
        cls_id = int(box.cls[0])
        class_name = model.model.names[cls_id]
        score = float(box.conf[0])
        is_defected = score > threshold

        final_results = {
            "status": "success",
            "is_defected": is_defected,
            "defect_type": class_name,
            "defect_percentage": score,
        }

    return jsonify(final_results), 200



@app.route('/update_threshold', methods=['POST'])
def update_threshold():
    global threshold

    data = request.get_json()

    if not data:
        return jsonify({'error': 'JSON body missing'}), 400

    if 'threshold' not in data:
        return jsonify({'error': 'Missing "threshold" field in JSON'}), 400

    threshold_value = data['threshold']
    threshold = threshold_value

    return jsonify({'message': 'Threshold received', 'threshold': threshold}), 200




if __name__ == '__main__':
    app.run(debug=True)
