from flask import Flask, request, jsonify
import os
import random

app = Flask(__name__)
UPLOAD_FOLDER = "uploads"
os.makedirs(UPLOAD_FOLDER, exist_ok=True)


@app.route('/upload_from_unity', methods=['POST'])
def upload_image():
    if 'image' not in request.files:
        return jsonify({"status": "error", "message": "No image part"}), 400

    image = request.files['image']
    if image.filename == '':
        return jsonify({"status": "error", "message": "No selected file"}), 400

    filepath = os.path.join(UPLOAD_FOLDER, image.filename)
    image.save(filepath)

    # Rastgele hata yüzdesi üret (0-100 arası)
    defect_percentage = random.randint(0, 100)

    # Eşik değeri (threshold) - %10'un altındakiler sağlam kabul edilecek
    threshold = 10

    # Sonuç belirleme
    is_defective = defect_percentage > threshold
    result_status = "defective" if is_defective else "okey"

    return jsonify({
        "status": "success",
        "result": result_status,
        "defect_percentage": defect_percentage,
        "threshold": threshold,
        "message": f"Image processed, defect: {defect_percentage}%"
    }), 200


if __name__ == '_main_':
    app.run(debug=True)
