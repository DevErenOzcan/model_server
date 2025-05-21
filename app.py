from flask import Flask, request
import os

app = Flask(__name__)
UPLOAD_FOLDER = "uploads"
os.makedirs(UPLOAD_FOLDER, exist_ok=True)

@app.route('/upload_from_unity', methods=['POST'])
def upload_image():
    if 'image' not in request.files:
        return "No image part", 400

    image = request.files['image']
    if image.filename == '':
        return "No selected file", 400

    filepath = os.path.join(UPLOAD_FOLDER, image.filename)
    image.save(filepath)
    return "Image saved", 200

if __name__ == '__main__':
    app.run(debug=True)
